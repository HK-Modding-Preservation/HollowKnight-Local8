using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    internal class Object
    {
        internal bool Destroyed;
        internal static Object[] World = Array.Empty<Object>();
        internal static int Scans;
        public static implicit operator bool(Object obj) => obj != null && !obj.Destroyed;
        internal static T[] FindObjectsOfType<T>() where T : Object
        {
            Scans++;
            return World.OfType<T>().Where(x => x && (!(x is Component c) || c.gameObject.activeInHierarchy)).ToArray();
        }
    }
    internal class Component : Object { internal GameObject gameObject; }
    internal class GameObject : Object
    {
        internal string name;
        internal bool activeInHierarchy = true;
        internal Component Special;
        internal T GetComponent<T>() where T : Component => Special as T;
    }
    internal class Collider2D : Component { }
    internal static class Physics2D
    {
        internal static readonly HashSet<(Collider2D, Collider2D)> Pairs = new();
        internal static void IgnoreCollision(Collider2D a, Collider2D b, bool ignore) { if (ignore) Pairs.Add((a, b)); }
    }
}
namespace KO.HollowKnight8
{
    internal sealed class KnightHatchling : UnityEngine.Component { }
    internal sealed class OwnerTag : UnityEngine.Component { internal PlayerSlot Player; }
    internal sealed class HeroController : UnityEngine.Object
    {
        internal UnityEngine.Collider2D[] Colliders;
        internal int Reads;
        internal T[] GetComponentsInChildren<T>(bool includeInactive) where T : UnityEngine.Component
        {
            if (!includeInactive) throw new Exception("Inactive collision pairs were omitted");
            Reads++;
            return (T[])(object)Colliders;
        }
    }
    internal sealed class PlayerSlot
    {
        internal int Index;
        internal bool Alive = true, Warping;
        internal HeroController Hero;
        internal UnityEngine.Collider2D[] Colliders;
    }
    internal static class EmergencyWarp { internal static bool Active(PlayerSlot p) => p.Warping; }
    internal sealed class Setting<T> { internal T Value; }
    internal sealed class Local8Runtime { internal Setting<bool> PerformanceMode, ReduceHitEffects=new(){Value=true}; internal PerformanceConfiguration Performance; }
    internal static class Plugin { internal static Local8Runtime Self; }
}
