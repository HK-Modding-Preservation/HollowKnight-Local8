using System;
using System.Collections.Generic;
using InControl;

namespace KO.HollowKnight8;

internal static class ProfileKeyboard
{
    internal static bool CanApply(PlayerSlot p,string saved,CoopSession session)
    {
        HeroActions candidate=null;
        try
        {
            candidate=new HeroActions();candidate.Load(saved);
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var action in candidate.Actions)foreach(var binding in action.Bindings)
                if(binding is KeyBindingSource)keys.Add(binding.Name);
            foreach(var other in session.Players)
                if(other!=p&&Controls.IsKeyboard(other.Device)&&other.Actions!=null)
                    foreach(var action in other.Actions.Actions)foreach(var binding in action.Bindings)
                        if(binding is KeyBindingSource&&keys.Contains(binding.Name))return false;
            return true;
        }
        catch(Exception e){Diagnostics.Throttled("PROFILE keyboard",e);return false;}
        finally{candidate?.Destroy();}
    }
    internal static void Apply(PlayerSlot p,string saved)
    {
        if(Local8Mod.Settings.ExtraKeyboardBindings==null||Local8Mod.Settings.ExtraKeyboardBindings.Length!=8)Local8Mod.Settings.ExtraKeyboardBindings=new string[8];
        p.Actions.Load(saved);KeypadInput.RepairMenus(p);p.Actions.ClearInputState();
        Local8Mod.Settings.ExtraKeyboardBindings[p.Index]=saved;
    }
}
