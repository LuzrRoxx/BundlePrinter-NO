using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Blueprinter.Ops
{
	internal static class EncyclopediaIncremental
	{
		internal static bool TryAdd(Encyclopedia enc, Object obj, ICollection<string> added = null)
		{
			bool flag = enc == null || obj == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				WeaponMount weaponMount = obj as WeaponMount;
				bool flag3 = weaponMount != null;
				if (flag3)
				{
					flag2 = EncyclopediaIncremental.TryAddWeaponMount(enc, weaponMount, added);
				}
				else
				{
					UnitDefinition unitDefinition = obj as UnitDefinition;
					bool flag4 = unitDefinition != null;
					flag2 = flag4 && EncyclopediaIncremental.TryAddUnit(enc, unitDefinition, added);
				}
			}
			return flag2;
		}

		private static bool TryAddUnit(Encyclopedia enc, UnitDefinition unit, ICollection<string> added)
		{
			bool flag = unit == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				string text;
				bool flag3 = !EncyclopediaIncremental.TryGetRequiredJsonKey(unit, out text);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = Encyclopedia.Lookup == null;
					if (flag4)
					{
						Encyclopedia.Lookup = new Dictionary<string, UnitDefinition>(EncyclopediaIncremental.JsonKeyComparer);
					}
					bool flag5 = enc.IndexLookup == null;
					if (flag5)
					{
						enc.IndexLookup = new List<INetworkDefinition>();
					}
					UnitDefinition unitDefinition;
					bool flag6 = Encyclopedia.Lookup.TryGetValue(text, out unitDefinition) && unitDefinition != unit;
					if (flag6)
					{
						Plugin.Log.LogError(string.Concat(new string[] { "[Encyclopedia] Illegal jsonKey collision '", text, "' between unit '", unitDefinition.name, "' and unit '", unit.name, "'. Skipping '", unit.name, "'." }));
						flag2 = false;
					}
					else
					{
						string text2;
						IList unitList = EncyclopediaIncremental.GetUnitList(enc, unit, out text2);
						bool flag7 = unitList == null;
						if (flag7)
						{
							Plugin.Log.LogWarning(string.Concat(new string[] { "[Encyclopedia] Destination list '", text2, "' was null for unit '", unit.name, "'; skipping." }));
							flag2 = false;
						}
						else
						{
							bool flag8 = false;
							bool flag9 = EncyclopediaIncremental.AddIfMissing(unitList, unit);
							if (flag9)
							{
								flag8 = true;
								try
								{
									unit.CacheMass();
								}
								catch (Exception ex)
								{
									Plugin.Log.LogError(string.Format("[Encyclopedia] CacheMass failed for '{0}': {1}", unit.name, ex));
								}
							}
							bool flag10 = !Encyclopedia.Lookup.ContainsKey(text);
							if (flag10)
							{
								Encyclopedia.Lookup[text] = unit;
								flag8 = true;
							}
							bool flag11 = EncyclopediaIncremental.AddToIndexIfMissing(enc.IndexLookup, unit);
							if (flag11)
							{
								flag8 = true;
							}
							bool flag12 = flag8;
							if (flag12)
							{
								if (added != null)
								{
									added.Add("Unit:" + unit.name);
								}
							}
							flag2 = flag8;
						}
					}
				}
			}
			return flag2;
		}

		private static bool TryAddWeaponMount(Encyclopedia enc, WeaponMount mount, ICollection<string> added)
		{
			bool flag = mount == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				string text;
				bool flag3 = !EncyclopediaIncremental.TryGetRequiredJsonKey(mount, out text);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = Encyclopedia.WeaponLookup == null;
					if (flag4)
					{
						Encyclopedia.WeaponLookup = new Dictionary<string, WeaponMount>(EncyclopediaIncremental.JsonKeyComparer);
					}
					bool flag5 = enc.IndexLookup == null;
					if (flag5)
					{
						enc.IndexLookup = new List<INetworkDefinition>();
					}
					WeaponMount weaponMount;
					bool flag6 = Encyclopedia.WeaponLookup.TryGetValue(text, out weaponMount) && weaponMount != mount;
					if (flag6)
					{
						Plugin.Log.LogError(string.Concat(new string[] { "[Encyclopedia] Illegal jsonKey collision '", text, "' between weapon mount '", weaponMount.name, "' and weapon mount '", mount.name, "'. Skipping '", mount.name, "'." }));
						flag2 = false;
					}
					else
					{
						bool flag7 = enc.weaponMounts == null;
						if (flag7)
						{
							Plugin.Log.LogWarning("[Encyclopedia] Destination list 'enc.weaponMounts' was null for weapon mount '" + mount.name + "'; skipping.");
							flag2 = false;
						}
						else
						{
							bool flag8 = false;
							bool flag9 = EncyclopediaIncremental.AddIfMissing(enc.weaponMounts, mount);
							if (flag9)
							{
								flag8 = true;
								try
								{
									mount.Initialize();
								}
								catch (Exception ex)
								{
									Plugin.Log.LogError(string.Format("[Encyclopedia] WeaponMount.Initialize failed for '{0}': {1}", mount.name, ex));
								}
							}
							bool flag10 = !Encyclopedia.WeaponLookup.ContainsKey(text);
							if (flag10)
							{
								Encyclopedia.WeaponLookup[text] = mount;
								flag8 = true;
							}
							bool flag11 = EncyclopediaIncremental.AddToIndexIfMissing(enc.IndexLookup, mount);
							if (flag11)
							{
								flag8 = true;
							}
							bool flag12 = flag8;
							if (flag12)
							{
								if (added != null)
								{
									added.Add("WeaponMount:" + mount.name);
								}
							}
							flag2 = flag8;
						}
					}
				}
			}
			return flag2;
		}

		private static IList GetUnitList(Encyclopedia enc, UnitDefinition unit, out string listName)
		{
			bool flag = unit is AircraftDefinition;
			IList list;
			if (flag)
			{
				listName = "enc.aircraft";
				list = enc.aircraft;
			}
			else
			{
				bool flag2 = unit is VehicleDefinition;
				if (flag2)
				{
					listName = "enc.vehicles";
					list = enc.vehicles;
				}
				else
				{
					bool flag3 = unit is MissileDefinition;
					if (flag3)
					{
						listName = "enc.missiles";
						list = enc.missiles;
					}
					else
					{
						bool flag4 = unit is BuildingDefinition;
						if (flag4)
						{
							listName = "enc.buildings";
							list = enc.buildings;
						}
						else
						{
							bool flag5 = unit is ShipDefinition;
							if (flag5)
							{
								listName = "enc.ships";
								list = enc.ships;
							}
							else
							{
								bool flag6 = unit is SceneryDefinition;
								if (flag6)
								{
									listName = "enc.scenery";
									list = enc.scenery;
								}
								else
								{
									listName = "enc.otherUnits";
									list = enc.otherUnits;
								}
							}
						}
					}
				}
			}
			return list;
		}

		private static bool AddIfMissing(IList list, object item)
		{
			bool flag = list == null || item == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = list.Contains(item);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					list.Add(item);
					flag2 = true;
				}
			}
			return flag2;
		}

		private static bool AddToIndexIfMissing(List<INetworkDefinition> index, INetworkDefinition net)
		{
			bool flag = index == null || net == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = EncyclopediaIncremental.ContainsReference(index, net);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					string text;
					bool flag4 = !EncyclopediaIncremental.TryGetRequiredJsonKey(net, out text);
					if (flag4)
					{
						flag2 = false;
					}
					else
					{
						EncyclopediaIncremental.TrackAddedIndexDefinition(net);
						index.Add(net);
						EncyclopediaIncremental.SortOnlyAddedIndexEntries(index);
						flag2 = true;
					}
				}
			}
			return flag2;
		}

		private static void TrackAddedIndexDefinition(INetworkDefinition def)
		{
			bool flag = def == null || EncyclopediaIncremental.ContainsReference(EncyclopediaIncremental.AddedIndexDefinitions, def);
			if (!flag)
			{
				EncyclopediaIncremental.AddedIndexDefinitions.Add(def);
			}
		}

		private static void SortOnlyAddedIndexEntries(List<INetworkDefinition> index)
		{
			bool flag = index == null || EncyclopediaIncremental.AddedIndexDefinitions.Count == 0;
			if (!flag)
			{
				List<int> list = new List<int>();
				List<INetworkDefinition> list2 = new List<INetworkDefinition>();
				for (int i = 0; i < index.Count; i++)
				{
					INetworkDefinition networkDefinition = index[i];
					bool flag2 = !EncyclopediaIncremental.ContainsReference(EncyclopediaIncremental.AddedIndexDefinitions, networkDefinition);
					if (!flag2)
					{
						list.Add(i);
						list2.Add(networkDefinition);
					}
				}
				bool flag3 = list2.Count < 1;
				if (!flag3)
				{
					list2.Sort(new Comparison<INetworkDefinition>(EncyclopediaIncremental.CompareDefinitionsDeterministically));
					for (int j = 0; j < list2.Count; j++)
					{
						INetworkDefinition networkDefinition2 = list2[j];
						int num = list[j];
						index[num] = networkDefinition2;
						networkDefinition2.LookupIndex = new int?(num);
					}
				}
			}
		}

		private static bool ContainsReference(IList<INetworkDefinition> list, INetworkDefinition item)
		{
			bool flag = list == null || item == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					bool flag3 = list[i] == item;
					if (flag3)
					{
						return true;
					}
				}
				flag2 = false;
			}
			return flag2;
		}

		private static int CompareDefinitionsDeterministically(INetworkDefinition a, INetworkDefinition b)
		{
			bool flag = a == b;
			int num;
			if (flag)
			{
				num = 0;
			}
			else
			{
				bool flag2 = a == null;
				if (flag2)
				{
					num = -1;
				}
				else
				{
					bool flag3 = b == null;
					if (flag3)
					{
						num = 1;
					}
					else
					{
						string requiredJsonKeyForSort = EncyclopediaIncremental.GetRequiredJsonKeyForSort(a);
						string requiredJsonKeyForSort2 = EncyclopediaIncremental.GetRequiredJsonKeyForSort(b);
						int num2 = EncyclopediaIncremental.JsonKeyComparer.Compare(requiredJsonKeyForSort, requiredJsonKeyForSort2);
						bool flag4 = num2 != 0;
						if (flag4)
						{
							num = num2;
						}
						else
						{
							int num3 = EncyclopediaIncremental.JsonKeyComparer.Compare(EncyclopediaIncremental.GetTypeSortKey(a), EncyclopediaIncremental.GetTypeSortKey(b));
							bool flag5 = num3 != 0;
							if (flag5)
							{
								Plugin.Log.LogError(string.Concat(new string[]
								{
									"[Encyclopedia] Duplicate jsonKey '",
									requiredJsonKeyForSort,
									"' detected across types '",
									EncyclopediaIncremental.GetTypeSortKey(a),
									"' and '",
									EncyclopediaIncremental.GetTypeSortKey(b),
									"'."
								}));
								num = num3;
							}
							else
							{
								Plugin.Log.LogError("[Encyclopedia] Duplicate jsonKey '" + requiredJsonKeyForSort + "' detected for multiple distinct objects in IndexLookup.");
								num = 0;
							}
						}
					}
				}
			}
			return num;
		}

		private static bool TryGetRequiredJsonKey(UnitDefinition unit, out string jsonKey)
		{
			jsonKey = null;
			bool flag = unit == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = unit != null && !string.IsNullOrWhiteSpace(((IHasJsonKey)unit).JsonKey);
				if (flag3)
				{
					jsonKey = ((IHasJsonKey)unit).JsonKey;
				}
				bool flag4 = string.IsNullOrWhiteSpace(jsonKey);
				if (flag4)
				{
					jsonKey = unit.jsonKey;
				}
				bool flag5 = string.IsNullOrWhiteSpace(jsonKey);
				if (flag5)
				{
					Plugin.Log.LogError("[Encyclopedia] Unit '" + unit.name + "' has no jsonKey. A non-empty jsonKey is required for deterministic network lookup.");
					flag2 = false;
				}
				else
				{
					flag2 = true;
				}
			}
			return flag2;
		}

		private static bool TryGetRequiredJsonKey(WeaponMount mount, out string jsonKey)
		{
			jsonKey = null;
			bool flag = mount == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = mount != null && !string.IsNullOrWhiteSpace(((IHasJsonKey)mount).JsonKey);
				if (flag3)
				{
					jsonKey = ((IHasJsonKey)mount).JsonKey;
				}
				bool flag4 = string.IsNullOrWhiteSpace(jsonKey);
				if (flag4)
				{
					jsonKey = mount.jsonKey;
				}
				bool flag5 = string.IsNullOrWhiteSpace(jsonKey);
				if (flag5)
				{
					Plugin.Log.LogError("[Encyclopedia] WeaponMount '" + mount.name + "' has no jsonKey. A non-empty jsonKey is required for deterministic network lookup.");
					flag2 = false;
				}
				else
				{
					flag2 = true;
				}
			}
			return flag2;
		}

		private static bool TryGetRequiredJsonKey(INetworkDefinition def, out string jsonKey)
		{
			jsonKey = null;
			bool flag = def == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				UnitDefinition unitDefinition = def as UnitDefinition;
				bool flag3 = unitDefinition != null;
				if (flag3)
				{
					flag2 = EncyclopediaIncremental.TryGetRequiredJsonKey(unitDefinition, out jsonKey);
				}
				else
				{
					WeaponMount weaponMount = def as WeaponMount;
					bool flag4 = weaponMount != null;
					if (flag4)
					{
						flag2 = EncyclopediaIncremental.TryGetRequiredJsonKey(weaponMount, out jsonKey);
					}
					else
					{
						IHasJsonKey hasJsonKey = def as IHasJsonKey;
						bool flag5 = hasJsonKey != null && !string.IsNullOrWhiteSpace(hasJsonKey.JsonKey);
						if (flag5)
						{
							jsonKey = hasJsonKey.JsonKey;
							flag2 = true;
						}
						else
						{
							Plugin.Log.LogError("[Encyclopedia] Definition type '" + EncyclopediaIncremental.GetTypeSortKey(def) + "' does not expose a valid jsonKey.");
							flag2 = false;
						}
					}
				}
			}
			return flag2;
		}

		private static string GetRequiredJsonKeyForSort(INetworkDefinition def)
		{
			string text;
			bool flag = EncyclopediaIncremental.TryGetRequiredJsonKey(def, out text);
			string text2;
			if (flag)
			{
				text2 = text;
			}
			else
			{
				text2 = string.Empty;
			}
			return text2;
		}

		private static string GetTypeSortKey(INetworkDefinition def)
		{
			bool flag = def == null;
			string text;
			if (flag)
			{
				text = string.Empty;
			}
			else
			{
				Type type = def.GetType();
				string text2;
				if (!(type != null))
				{
					text2 = string.Empty;
				}
				else if ((text2 = type.FullName) == null)
				{
					text2 = type.Name ?? string.Empty;
				}
				text = text2;
			}
			return text;
		}

		private static readonly StringComparer JsonKeyComparer = StringComparer.Ordinal;

		private static readonly List<INetworkDefinition> AddedIndexDefinitions = new List<INetworkDefinition>();
	}
}
