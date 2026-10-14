using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace KO.HollowKnight8;

// Optional integration. No Pale Court/SFCore binary or copied mod source is
// required. Only the exact lifecycle/serialization failures in the log are
// patched; unrelated third-party components and native Start still run.
public static class PaleCourtInterop
{
    private static readonly List<ILHook> hooks = new List<ILHook>();
    private static Type boonType;
    private static bool checkedAssembly;

    internal static void Prepare()
    {
        if (checkedAssembly) return;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type amulets = assembly.GetType("FiveKnights.Amulets", false);
            if (amulets == null) continue;
            checkedAssembly = true;
            try
            {
                Type stage = assembly.GetType("FiveKnights.CheckBloomStage", true);
                Type bloom = assembly.GetType("FiveKnights.AbyssalBloom", true);
                MethodInfo start = amulets.GetMethod("On_HeroController_Start", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo check = stage.GetMethod("CheckStage", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo level = bloom.GetMethod("SetLevel", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo slashes = bloom.GetField("_nailSlashes", BindingFlags.Instance | BindingFlags.NonPublic);
                Type boon = assembly.GetType("FiveKnights.BoonSpells", true);
                if (start == null || check == null || level == null || slashes == null ||
                    start.ReturnType != typeof(void) || start.GetParameters().Length != 2 ||
                    start.GetParameters()[0].ParameterType != typeof(On.HeroController.orig_Start) ||
                    start.GetParameters()[1].ParameterType != typeof(HeroController) ||
                    check.ReturnType != typeof(void) || check.GetParameters().Length != 0 ||
                    level.ReturnType != typeof(void) || level.GetParameters().Length != 1 ||
                    level.GetParameters()[0].ParameterType != typeof(int))
                    throw new MissingMethodException("Unrecognized Pale Court charm lifecycle");

                hooks.Add(new ILHook(start, il =>
                {
                    var c = new ILCursor(il);
                    Instruction body = c.Next;
                    c.Emit(OpCodes.Ldarg_2);
                    c.Emit(OpCodes.Call, Method(nameof(ExtraHero)));
                    c.Emit(OpCodes.Brfalse, body);
                    // Preserve the entire original HeroController.Start chain.
                    c.Emit(OpCodes.Ldarg_1);
                    c.Emit(OpCodes.Ldarg_2);
                    c.Emit(OpCodes.Callvirt, typeof(On.HeroController.orig_Start).GetMethod("Invoke"));
                    c.Emit(OpCodes.Ret);
                }));
                hooks.Add(new ILHook(check, il =>
                {
                    var c = new ILCursor(il);
                    Instruction body = c.Next;
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Call, Method(nameof(WrongActor)));
                    c.Emit(OpCodes.Brfalse, body);
                    c.Emit(OpCodes.Ret);
                }));
                hooks.Add(new ILHook(level, il =>
                {
                    var c = new ILCursor(il);
                    Instruction body = c.Next;
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldarg_1);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldfld, slashes);
                    c.Emit(OpCodes.Call, Method(nameof(UninitializedBloom)));
                    c.Emit(OpCodes.Brfalse, body);
                    c.Emit(OpCodes.Ret);
                }));
                boonType = boon;
                InstallBoonTemplateGuard(boon);
                Diagnostics.Write("PALE COURT scoped clone/start and bloom guards installed");
            }
            catch (Exception ex)
            {
                ReleaseHooks();
                Diagnostics.Throttled("PALE COURT optional integration", ex);
            }
            return;
        }
        // Do not latch absence: optional mods can initialize later.
    }

    private static MethodInfo Method(string name)
        => typeof(PaleCourtInterop).GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static void InstallBoonTemplateGuard(Type boon)
    {
        MethodInfo enable = boon.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        if (enable == null) return;
        try
        {
            // Pale Court copies PV to read prefab actions and sound clips on
            // every enable. The temporary boss used to remain under the knight:
            // the log gained about 32 FSMs per room, plus its live visuals.
            // Match that exact generic Instantiate call, not arbitrary prefabs.
            hooks.Add(new ILHook(enable, il =>
            {
                var c = new ILCursor(il);
                if (!c.TryGotoNext(MoveType.After, instruction =>
                    instruction.Operand is Mono.Cecil.GenericInstanceMethod method &&
                    method.DeclaringType.FullName == "UnityEngine.Object" && method.Name == "Instantiate" &&
                    method.GenericArguments.Count == 1 && method.GenericArguments[0].FullName == typeof(PlayMakerFSM).FullName &&
                    method.Parameters.Count == 2 && method.Parameters[1].ParameterType.FullName == typeof(Transform).FullName))
                    throw new MissingMethodException("Unrecognized Pale Court Boon template constructor");
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Call, Method(nameof(DiscardBoonTemplate)));
            }));
        }
        catch (Exception ex) { Diagnostics.Throttled("PALE COURT optional Boon template guard", ex); }
    }

    public static void DiscardBoonTemplate(PlayMakerFSM template)
    {
        CoopSession s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (s == null || !s.Active || !template || template.gameObject.GetComponent<HeroController>()) return;
        PlayerSlot p = s.Resolve(template);
        if (p == null || !p.Hero || template.gameObject == p.Hero.gameObject || !template.transform.IsChildOf(p.Hero.transform)) return;
        // Destroy is deferred: OnEnable can finish reading actions/clips this
        // frame. Disabling now prevents the copied boss from running meanwhile.
        template.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(template.gameObject);
    }

    // Public visibility is required: emitted IL in the optional provider's
    // assembly calls these guards. Do not depend on Mono bypassing access checks.
    public static bool ExtraHero(HeroController hero)
    {
        CoopSession s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (s == null || !s.Active || s.Primary == null || !hero || hero == s.Primary.Hero) return false;
        PlayerSlot p = s.Resolve(hero);
        return p != null && p.Index > 0 || s.Cloning != null;
    }

    public static bool WrongActor(Component component)
    {
        CoopSession s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (s == null || !s.Active || !component) return false;
        PlayerSlot p = s.Resolve(component);
        return p != null && p.Hero && HeroController.instance != p.Hero;
    }

    public static bool UninitializedBloom(Component component, int level, object slashes)
    {
        if (WrongActor(component)) return true;
        CoopSession s = Plugin.Self == null ? null : Plugin.Self.Session;
        return s != null && s.Active && level == 0 && slashes == null;
    }

    internal static void RepairClone(CoopSession session, PlayerSlot player)
    {
        try { RepairCallbacks(session, player); }
        catch (Exception ex) { Diagnostics.Throttled("PALE COURT optional clone repair", ex); }
    }

    private static void RepairCallbacks(CoopSession session, PlayerSlot player)
    {
        if (boonType == null || player == null || player.Index == 0 || !player.Hero || session.Primary == null || !session.Primary.Hero) return;
        PlayMakerFSM from = session.Primary.Hero.spellControl;
        PlayMakerFSM to = player.Hero.spellControl;
        if (!from || !to || from == to) return;
        int repaired = 0;
        foreach (FsmState state in to.FsmStates)
        {
            string method;
            bool upgraded;
            if (!BoonState(state.Name, out method, out upgraded)) continue;
            FsmState original = null;
            foreach (FsmState candidate in from.FsmStates) if (candidate.Name == state.Name) { original = candidate; break; }
            if (original == null || original.Actions == null || state.Actions == null || original.Actions.Length != state.Actions.Length) continue;
            for (int i = 0; i < original.Actions.Length; i++)
            {
                FsmStateAction source = original.Actions[i], target = state.Actions[i];
                if (source == null || !CustomCallback(source.GetType().Name) ||
                    target != null && target.GetType().Name != "MissingAction") continue;
                MethodInfo callback = boonType.GetMethod(method, BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
                if (callback == null) continue;
                var replacement = new BoonAction(player, callback, upgraded);
                replacement.Init(state);
                state.Actions[i] = replacement;
                repaired++;
            }
        }
        if (repaired > 0) Diagnostics.Write("PALE COURT restored un-serializable Boon callbacks P" + (player.Index + 1) + " count=" + repaired);
    }

    private static bool CustomCallback(string name) => name == "InvokeMethod" || name == "MethodAction";

    internal static bool BoonState(string state, out string method, out bool upgraded)
    {
        upgraded = false;
        switch (state)
        {
            case "Fireball 2 SmallShots": upgraded = true; goto case "Fireball 1 SmallShots";
            case "Fireball 1 SmallShots": method = "CastDaggers"; return true;
            case "Q2 Land Plumes": upgraded = true; goto case "Q1 Land Plumes";
            case "Q1 Land Plumes": method = "CastPlumes"; return true;
            case "Scream Burst 2 Blasts": upgraded = true; goto case "Scream Burst 1 Blasts";
            case "Scream Burst 1 Blasts": method = "CastBlasts"; return true;
            default: method = null; return false;
        }
    }

    private sealed class BoonAction : FsmStateAction
    {
        private readonly PlayerSlot player;
        private readonly MethodInfo callback;
        private readonly object[] arguments;
        internal BoonAction(PlayerSlot p, MethodInfo method, bool upgraded) { player = p; callback = method; arguments = new object[] { upgraded }; }
        public override void OnEnter()
        {
            try
            {
                if (player.Hero && player.Alive && player.Ready)
                {
                    Component component = player.Hero.GetComponent(boonType);
                    if (component) using (PlayerContext.Enter(player)) callback.Invoke(component, arguments);
                }
            }
            catch (Exception ex) { Diagnostics.Throttled("PALE COURT spell callback", ex); }
            finally { Finish(); }
        }
    }

    private static void ReleaseHooks()
    {
        for (int i = hooks.Count - 1; i >= 0; i--)
        {
            try { hooks[i].Dispose(); }
            catch (Exception ex) { Diagnostics.Throttled("PALE COURT hook cleanup", ex); }
        }
        hooks.Clear();
        boonType = null;
    }

    internal static void Uninstall() { ReleaseHooks(); checkedAssembly = false; }
}
