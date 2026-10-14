using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace KO.HollowKnight8;

// Cache executable field access, never its value. Every context/action still
// reads and writes the live reference, including another mod's same-frame edits.
// Desktop Mono supports DynamicMethod; reflection remains the fallback.
internal sealed class ReferenceField
{
    internal enum BindingKind { Reference, HeroState, Data, Actions }
    private static readonly Dictionary<FieldInfo, ReferenceField> fields = new Dictionary<FieldInfo, ReferenceField>();
    private readonly FieldInfo nativeField;
    private Func<object, object> getter;
    private Action<object, object> setter;
    internal static int Compiled, Fallback;
    internal readonly Type FieldType;
    internal readonly BindingKind Binding;
    internal bool FastGet => getter != null;
    internal bool FastSet => setter != null;

    internal static ReferenceField For(FieldInfo field)
    {
        if (field == null) return null;
        ReferenceField result;
        if (!fields.TryGetValue(field, out result)) fields[field] = result = new ReferenceField(field);
        return result;
    }

    internal ReferenceField(FieldInfo field, bool compile = true)
    {
        nativeField = field;
        FieldType = field == null ? null : field.FieldType;
        Binding = FieldType == typeof(HeroControllerStates) ? BindingKind.HeroState :
            FieldType == typeof(PlayerData) ? BindingKind.Data :
            FieldType == typeof(HeroActions) ? BindingKind.Actions : BindingKind.Reference;
        if (compile && field != null && !field.IsLiteral && !field.FieldType.IsValueType && !field.FieldType.IsPointer &&
            !field.DeclaringType.IsValueType && !field.DeclaringType.ContainsGenericParameters && !field.FieldType.ContainsGenericParameters)
        {
            try
            {
                var read = new DynamicMethod("Local8_read_" + field.Name, typeof(object), new[] { typeof(object) }, field.Module, true);
                ILGenerator il = read.GetILGenerator();
                if (!field.IsStatic) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, field.DeclaringType); }
                if (Volatile(field)) il.Emit(OpCodes.Volatile);
                il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field); il.Emit(OpCodes.Ret);
                getter = (Func<object, object>)read.CreateDelegate(typeof(Func<object, object>));
                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    var write = new DynamicMethod("Local8_write_" + field.Name, typeof(void), new[] { typeof(object), typeof(object) }, field.Module, true);
                    il = write.GetILGenerator();
                    if (!field.IsStatic) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, field.DeclaringType); }
                    il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Castclass, field.FieldType);
                    if (Volatile(field)) il.Emit(OpCodes.Volatile);
                    il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field); il.Emit(OpCodes.Ret);
                    setter = (Action<object, object>)write.CreateDelegate(typeof(Action<object, object>));
                }
            }
            catch (Exception) { getter = null; setter = null; }
        }
        if (getter != null) Compiled++; else Fallback++;
    }

    private static bool Volatile(FieldInfo field)
    {
        foreach (Type type in field.GetRequiredCustomModifiers())
            if (type == typeof(System.Runtime.CompilerServices.IsVolatile)) return true;
        return false;
    }

    internal object GetValue(object target)
    {
        if (getter != null)
        {
            try { return getter(target); }
            catch (MemberAccessException) { DisableFast(); }
            catch (SecurityException) { DisableFast(); }
            catch (NotSupportedException) { DisableFast(); }
            catch (InvalidCastException) { return nativeField.GetValue(target); }
            catch (NullReferenceException) { return nativeField.GetValue(target); }
        }
        return nativeField.GetValue(target);
    }

    internal void SetValue(object target, object value)
    {
        if (setter != null)
        {
            try { setter(target, value); return; }
            catch (MemberAccessException) { DisableFast(); }
            catch (SecurityException) { DisableFast(); }
            catch (NotSupportedException) { DisableFast(); }
            catch (InvalidCastException) { nativeField.SetValue(target, value); return; }
            catch (NullReferenceException) { nativeField.SetValue(target, value); return; }
        }
        nativeField.SetValue(target, value);
    }

    private void DisableFast()
    {
        if (getter != null) { Compiled--; Fallback++; }
        getter = null; setter = null;
    }
}
