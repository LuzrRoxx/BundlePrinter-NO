using System;
using HarmonyLib;

namespace Blueprinter
{
	[HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[] { })]
	public static class EncyclopediaAfterLoadPatch
	{
		private static void Prefix(Encyclopedia __instance)
		{
			bool flag = Plugin.Instance == null;
			if (flag)
			{
				Plugin.Log.LogError("Plugin instance is null!");
			}
			else
			{
				bool runOnce = EncyclopediaAfterLoadPatch.RunOnce;
				if (!runOnce)
				{
					EncyclopediaAfterLoadPatch.RunOnce = true;
					Plugin.Instance._encyclopedia = __instance;
					Plugin.Instance.StartCoroutine(Plugin.Instance.RunRoutine());
				}
			}
		}

		internal static bool RunOnce;
	}
}
