using UnityEngine;

namespace KO.HollowKnight8;

internal static class DuelGround
{
	private static readonly Collider2D[] hits = new Collider2D[256];

	private static readonly RaycastHit2D[] floorHits = new RaycastHit2D[32];

	internal static Bounds Body(PlayerSlot p)
	{
		BoxCollider2D component = p.Hero.GetComponent<BoxCollider2D>();
		if ((bool)component)
		{
			Vector3 horizontal = component.transform.TransformVector(new Vector3(component.size.x, 0, 0));
			Vector3 vertical = component.transform.TransformVector(new Vector3(0, component.size.y, 0));
			return new Bounds(component.transform.TransformPoint(component.offset), new Vector3(Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x), Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y), 0.1f));
		}
		Collider2D component2 = p.Hero.GetComponent<Collider2D>();
		if ((bool)component2 && component2.enabled && component2.bounds.size.y > 0.1f)
		{
			return component2.bounds;
		}
		return new Bounds(p.Hero.transform.position, new Vector3(0.8f, 2.4f, 0.1f));
	}

	internal static bool Safe(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		if (!Clear(p, at, offset, size, out reason))
		{
			return false;
		}
		return Supported(at, offset, size, out reason);
	}

	internal static bool Supported(Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		reason = "";
		Vector2 vector = at + offset;
		bool flag = false;
		float num = vector.y - size.y * 0.5f;
		for (int i = -1; i <= 1; i++)
		{
			if (flag)
			{
				break;
			}
			int num2 = Physics2D.RaycastNonAlloc(new Vector2(vector.x + (float)i * size.x * 0.25f, num + 0.15f), Vector2.down, floorHits, 0.65f, (1 << 8) | (1 << 25));
			if (num2 == floorHits.Length) { reason = "suelo demasiado ocupado"; return false; }
			for (int j = 0; j < num2; j++)
			{
				if (SpawnSafety.Solid(floorHits[j].collider) && floorHits[j].normal.y > 0.45f)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			reason = "sin suelo bajo los pies";
		}
		return flag;
	}

	internal static bool Clear(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		return ClearBody(p, at, offset, size, arena: false, out reason);
	}

	internal static bool ClearArena(PlayerSlot p, Vector3 at, out string reason)
	{
		Bounds bounds = Body(p);
		return ClearBody(p, at, bounds.center - p.Hero.transform.position, bounds.size, arena: true, out reason);
	}

	private static bool ClearBody(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, bool arena, out string reason)
	{
		reason = "";
		if (!SpawnSafety.BodyInside(at, offset, size)) { reason = "fuera de escena"; return false; }
		Vector2 point = at + offset;
		Vector2 size2 = SpawnSafety.Clearance(size);
		int num = Physics2D.OverlapBoxNonAlloc(point, size2, 0f, hits);
		if (num == hits.Length)
		{
			reason = "zona demasiado ocupada";
			return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
		}
		for (int i = 0; i < num; i++)
		{
			Collider2D collider2D = hits[i];
			if (!collider2D || !collider2D.enabled || (bool)collider2D.GetComponentInParent<HeroController>())
			{
				continue;
			}
			if ((collider2D.gameObject.layer == 8 || collider2D.gameObject.layer == 25) && !collider2D.isTrigger)
			{
				reason = "pared: " + collider2D.name;
				return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
			}
			DamageHero component = collider2D.GetComponent<DamageHero>();
			if ((bool)component && (component.damageDealt > 0 || component.hazardType > 1) && (!arena || component.hazardType > 1))
			{
				reason = "peligro: " + collider2D.name + " type=" + component.hazardType;
				return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
			}
			if (FSMUtility.ContainsFSM(collider2D.gameObject, "damages_hero"))
			{
				PlayMakerFSM fsm = FSMUtility.LocateFSM(collider2D.gameObject, "damages_hero");
				if (!arena || FSMUtility.GetInt(fsm, "hazardType") > 1)
				{
					reason = "peligro FSM: " + collider2D.name;
					return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
				}
			}
		}
		return SpawnSafety.Footprint(normal: true, p, at, offset, size, arena);
	}
}
