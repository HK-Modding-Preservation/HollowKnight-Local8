using System;
using System.Collections.Generic;
using System.Linq;

namespace KO.HollowKnight8;

internal sealed class CharmLoadout
{
	internal readonly bool[] Equipped = new bool[40];

	internal readonly HashSet<int> Extra = new HashSet<int>();

	internal bool IsEquipped(int id)
	{
		return id > 0 && (id <= 40 ? Equipped[id - 1] : Extra.Contains(id));
	}

	internal List<int> Order = new List<int>();

	// The provider/save code may edit PlayerData.equippedCharms in place. Keep
	// a detached slot snapshot and a separate reusable list for each Apply.
	private readonly List<int> extraOrder = new List<int>();
	private readonly List<int> appliedOrder = new List<int>();

	internal int Used;

	internal bool Overcharmed;

	internal void Read(PlayerData data)
	{
		Equipped[0] = data.equippedCharm_1;
		Equipped[1] = data.equippedCharm_2;
		Equipped[2] = data.equippedCharm_3;
		Equipped[3] = data.equippedCharm_4;
		Equipped[4] = data.equippedCharm_5;
		Equipped[5] = data.equippedCharm_6;
		Equipped[6] = data.equippedCharm_7;
		Equipped[7] = data.equippedCharm_8;
		Equipped[8] = data.equippedCharm_9;
		Equipped[9] = data.equippedCharm_10;
		Equipped[10] = data.equippedCharm_11;
		Equipped[11] = data.equippedCharm_12;
		Equipped[12] = data.equippedCharm_13;
		Equipped[13] = data.equippedCharm_14;
		Equipped[14] = data.equippedCharm_15;
		Equipped[15] = data.equippedCharm_16;
		Equipped[16] = data.equippedCharm_17;
		Equipped[17] = data.equippedCharm_18;
		Equipped[18] = data.equippedCharm_19;
		Equipped[19] = data.equippedCharm_20;
		Equipped[20] = data.equippedCharm_21;
		Equipped[21] = data.equippedCharm_22;
		Equipped[22] = data.equippedCharm_23;
		Equipped[23] = data.equippedCharm_24;
		Equipped[24] = data.equippedCharm_25;
		Equipped[25] = data.equippedCharm_26;
		Equipped[26] = data.equippedCharm_27;
		Equipped[27] = data.equippedCharm_28;
		Equipped[28] = data.equippedCharm_29;
		Equipped[29] = data.equippedCharm_30;
		Equipped[30] = data.equippedCharm_31;
		Equipped[31] = data.equippedCharm_32;
		Equipped[32] = data.equippedCharm_33;
		Equipped[33] = data.equippedCharm_34;
		Equipped[34] = data.equippedCharm_35;
		Equipped[35] = data.equippedCharm_36;
		Equipped[36] = data.equippedCharm_37;
		Equipped[37] = data.equippedCharm_38;
		Equipped[38] = data.equippedCharm_39;
		Equipped[39] = data.equippedCharm_40;
		List<int> source = data.equippedCharms;
		if (source != null) foreach (int id in source)
			if (id > 40 && !CharmExtensions.Contains(id) && CharmExtensions.Owned(data, id)) CharmExtensions.Register(id);
		Extra.Clear();
		CharmExtensions.Entry[] extra = CharmExtensions.Entries;
		if (extra.Length == 0) Order = source ?? new List<int>();
		else
		{
			if (!ReferenceEquals(source, extraOrder))
			{
				extraOrder.Clear();
				if (source != null) extraOrder.AddRange(source);
			}
			Order = extraOrder;
		}
		foreach (CharmExtensions.Entry entry in extra)
		{
			if (CharmExtensions.Equipped(data, entry))
			{
				Extra.Add(entry.Id);
				if (!Order.Contains(entry.Id)) Order.Add(entry.Id);
			}
			else
			{
				for (int i = Order.Count - 1; i >= 0; i--) if (Order[i] == entry.Id) Order.RemoveAt(i);
			}
		}
		Used = data.charmSlotsFilled;
		Overcharmed = data.overcharmed;
	}

	internal void Write(PlayerData data)
	{
		data.equippedCharm_1 = Equipped[0];
		data.equippedCharm_2 = Equipped[1];
		data.equippedCharm_3 = Equipped[2];
		data.equippedCharm_4 = Equipped[3];
		data.equippedCharm_5 = Equipped[4];
		data.equippedCharm_6 = Equipped[5];
		data.equippedCharm_7 = Equipped[6];
		data.equippedCharm_8 = Equipped[7];
		data.equippedCharm_9 = Equipped[8];
		data.equippedCharm_10 = Equipped[9];
		data.equippedCharm_11 = Equipped[10];
		data.equippedCharm_12 = Equipped[11];
		data.equippedCharm_13 = Equipped[12];
		data.equippedCharm_14 = Equipped[13];
		data.equippedCharm_15 = Equipped[14];
		data.equippedCharm_16 = Equipped[15];
		data.equippedCharm_17 = Equipped[16];
		data.equippedCharm_18 = Equipped[17];
		data.equippedCharm_19 = Equipped[18];
		data.equippedCharm_20 = Equipped[19];
		data.equippedCharm_21 = Equipped[20];
		data.equippedCharm_22 = Equipped[21];
		data.equippedCharm_23 = Equipped[22];
		data.equippedCharm_24 = Equipped[23];
		data.equippedCharm_25 = Equipped[24];
		data.equippedCharm_26 = Equipped[25];
		data.equippedCharm_27 = Equipped[26];
		data.equippedCharm_28 = Equipped[27];
		data.equippedCharm_29 = Equipped[28];
		data.equippedCharm_30 = Equipped[29];
		data.equippedCharm_31 = Equipped[30];
		data.equippedCharm_32 = Equipped[31];
		data.equippedCharm_33 = Equipped[32];
		data.equippedCharm_34 = Equipped[33];
		data.equippedCharm_35 = Equipped[34];
		data.equippedCharm_36 = Equipped[35];
		data.equippedCharm_37 = Equipped[36];
		data.equippedCharm_38 = Equipped[37];
		data.equippedCharm_39 = Equipped[38];
		data.equippedCharm_40 = Equipped[39];
		CharmExtensions.Entry[] extra = CharmExtensions.Entries;
		foreach (CharmExtensions.Entry entry in extra)
		{
			bool equipped = Extra.Contains(entry.Id);
			CharmExtensions.Equip(data, entry, equipped);
		}
		if (extra.Length == 0) data.equippedCharms = Order;
		else
		{
			appliedOrder.Clear();
			appliedOrder.AddRange(Order);
			data.equippedCharms = appliedOrder;
		}
		data.charmSlotsFilled = Used;
		data.overcharmed = Overcharmed;
	}

	internal int[] Ids()
	{
		return (from i in Enumerable.Range(1, 40)
			where Equipped[i - 1]
			select i).Concat(Extra.OrderBy(id => id)).ToArray();
	}

	internal void Initialize(PlayerData data, int[] ids)
	{
		Array.Clear(Equipped, 0, 40);
		Extra.Clear();
		CharmExtensions.Discover();
		Order = new List<int>();
		Used = 0;
		int[] array = ids ?? new int[0];
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i];
			if (num >= 1 && !IsEquipped(num) && CharmExtensions.Owned(data, num))
			{
				int num2 = Math.Max(0, data.GetInt("charmCost_" + num));
				if (CoopRules.CanEquip(Used, num2, data.charmSlots, data.canOvercharm))
				{
					if (num <= 40) Equipped[num - 1] = true;
					else { CharmExtensions.Register(num); Extra.Add(num); }
					Order.Add(num);
					Used += num2;
				}
			}
		}
		Overcharmed = Used > data.charmSlots;
	}
}
