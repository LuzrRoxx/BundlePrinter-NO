using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blueprinter.Ops
{
	public sealed class OpAddWeaponMountToWeaponManagerHandler : PostOpHandlerBase<OpAddWeaponMountPayload>
	{
		public override string opId
		{
			get
			{
				return "OpAddWeaponMountToWeaponManager";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpAddWeaponMountPayload payload)
		{
			bool flag = payload.bundleAsset == null || payload.weaponManagers == null || payload.weaponManagers.Length == 0;
			if (!flag)
			{
				Object @object = ResourcesAssetResolver.ResolveBundleAsset(bundle, payload.bundleAsset);
				WeaponMount weaponMount = @object as WeaponMount;
				bool flag2 = weaponMount == null;
				if (!flag2)
				{
					foreach (WeaponManagerTarget weaponManagerTarget in payload.weaponManagers)
					{
						bool flag3 = ((weaponManagerTarget != null) ? weaponManagerTarget.gameAsset : null) == null;
						if (!flag3)
						{
							WeaponManager weaponManager = ResourcesAssetResolver.ResolveGameAsset(weaponManagerTarget.gameAsset) as WeaponManager;
							bool flag4 = weaponManager == null || weaponManager.hardpointSets == null || weaponManager.hardpointSets.Length == 0;
							if (!flag4)
							{
								List<int> list = OpAddWeaponMountToWeaponManagerHandler.ResolveHardpointIndices(weaponManager, weaponManagerTarget.hardpointSetIndices);
								bool flag5 = list.Count == 0;
								if (!flag5)
								{
									foreach (int num in list)
									{
										HardpointSet hardpointSet = weaponManager.hardpointSets[num];
										bool flag6 = hardpointSet == null;
										if (!flag6)
										{
											HardpointSet hardpointSet2 = hardpointSet;
											if (hardpointSet2.weaponOptions == null)
											{
												hardpointSet2.weaponOptions = new List<WeaponMount>();
											}
											bool flag7 = hardpointSet.weaponOptions.Contains(weaponMount);
											if (!flag7)
											{
												hardpointSet.weaponOptions.Add(weaponMount);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static List<int> ResolveHardpointIndices(WeaponManager weaponManager, int[] indices)
		{
			bool flag = ((weaponManager != null) ? weaponManager.hardpointSets : null) == null;
			List<int> list;
			if (flag)
			{
				list = new List<int>();
			}
			else
			{
				List<int> list2 = new List<int>();
				bool flag2 = indices == null || indices.Length == 0;
				if (flag2)
				{
					list = list2;
				}
				else
				{
					HashSet<int> hashSet = new HashSet<int>();
					foreach (int num in indices)
					{
						bool flag3 = num < 0 || num >= weaponManager.hardpointSets.Length;
						if (!flag3)
						{
							bool flag4 = hashSet.Add(num);
							if (flag4)
							{
								list2.Add(num);
							}
						}
					}
					list = list2;
				}
			}
			return list;
		}

		public const string OpId = "OpAddWeaponMountToWeaponManager";
	}
}
