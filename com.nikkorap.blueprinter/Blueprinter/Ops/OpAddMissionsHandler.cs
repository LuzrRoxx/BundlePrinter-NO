using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NuclearOption.SavedMission;
using UnityEngine;

namespace Blueprinter.Ops
{
	public sealed class OpAddMissionsHandler : PostOpHandlerBase<OpAddMissionsPayload>
	{
		public override string opId
		{
			get
			{
				return "OpAddMissions";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpAddMissionsPayload payload)
		{
			bool flag = payload == null || payload.MissionAssets == null || payload.MissionGroups == null || payload.MissionGroups.Length == 0;
			if (!flag)
			{
				TextAsset[] array = payload.MissionAssets.Select<AssetRef, TextAsset>((AssetRef asset) => ResourcesAssetResolver.ResolveBundleAsset(bundle, asset) as TextAsset).ToArray<TextAsset>();
				bool flag2 = array.Length == 0;
				if (!flag2)
				{
					foreach (string text in payload.MissionGroups)
					{
						MissionGroup.ResourceGroup resourceGroup;
						bool flag3 = !OpAddMissionsHandler.resourceGroups.TryGetValue(text, out resourceGroup) || resourceGroup == null;
						if (!flag3)
						{
							foreach (TextAsset textAsset in array)
							{
								OpAddMissionsHandler.AddMission(resourceGroup, textAsset, text);
							}
						}
					}
				}
			}
		}

		public static void AddMission(MissionGroup.ResourceGroup groupAsset, TextAsset missionAsset, string groupName)
		{
			bool flag = groupAsset == null || missionAsset == null || string.IsNullOrWhiteSpace(groupName);
			if (!flag)
			{
				TextAsset[] array = (TextAsset[])OpAddMissionsHandler.AssetsField.GetValue(groupAsset);
				bool flag2 = array == null || array.Length == 0;
				if (!flag2)
				{
					MissionKey[] array2 = (MissionKey[])OpAddMissionsHandler.NamesField.GetValue(groupAsset);
					bool flag3 = array2 == null || array2.Length == 0;
					if (!flag3)
					{
						MissionKey missionKey = new MissionKey(missionAsset.name, groupAsset);
						OpAddMissionsHandler.AssetsField.SetValue(groupAsset, array.Append(missionAsset).ToArray<TextAsset>());
						OpAddMissionsHandler.NamesField.SetValue(groupAsset, array2.Append(missionKey).ToArray<MissionKey>());
						Plugin.Log.LogDebug("[OpAddMissions] ADDED " + missionAsset.name + " TO " + groupName);
					}
				}
			}
		}

		public const string OpId = "OpAddMissions";

		private static Dictionary<string, MissionGroup.ResourceGroup> resourceGroups = new Dictionary<string, MissionGroup.ResourceGroup>();

		private static readonly FieldInfo AssetsField = AccessTools.Field(typeof(MissionGroup.ResourceGroup), "assets");

		private static readonly FieldInfo NamesField = AccessTools.Field(typeof(MissionGroup.ResourceGroup), "names");

		[HarmonyPatch(typeof(MissionGroup.ResourceGroup), MethodType.Constructor, new Type[]
		{
			typeof(string),
			typeof(string)
		})]
		private static class ResourceGroupAddMissions
		{
			[HarmonyPrefix]
			private static void Prefix(MissionGroup.ResourceGroup __instance, string name, string path, ref TextAsset[] ___assets, ref MissionKey[] ___names, ref string ___Name)
			{
				bool flag = name == "Free Flight";
				if (flag)
				{
					OpAddMissionsHandler.resourceGroups.Add("FreeFlight", __instance);
				}
				else
				{
					OpAddMissionsHandler.resourceGroups.Add(name, __instance);
				}
			}
		}
	}
}
