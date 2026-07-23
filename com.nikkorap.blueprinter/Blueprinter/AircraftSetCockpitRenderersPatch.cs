using System;
using HarmonyLib;
using UnityEngine;

namespace Blueprinter
{
	[HarmonyPatch(typeof(Aircraft), "SetCockpitRenderers")]
	internal static class AircraftSetCockpitRenderersPatch
	{
		private static bool Prefix(Aircraft __instance, bool enabled)
		{
			bool flag = AircraftSetCockpitRenderersPatch.cockpitRenderersref.Invoke(__instance) == null;
			bool flag2;
			if (flag)
			{
				flag2 = true;
			}
			else
			{
				foreach (Renderer renderer in AircraftSetCockpitRenderersPatch.cockpitRenderersref.Invoke(__instance))
				{
					bool flag3 = renderer == null;
					if (flag3)
					{
						Plugin.Log.LogWarning("cockpitRenderersref NULL");
					}
					else
					{
						renderer.enabled = enabled;
					}
				}
				bool flag4 = AircraftSetCockpitRenderersPatch.exteriorRenderersref.Invoke(__instance) == null;
				if (flag4)
				{
					flag2 = true;
				}
				else
				{
					foreach (Renderer renderer2 in AircraftSetCockpitRenderersPatch.exteriorRenderersref.Invoke(__instance))
					{
						bool flag5 = renderer2 == null;
						if (flag5)
						{
							Plugin.Log.LogWarning("exteriorRenderersref NULL");
						}
						else
						{
							renderer2.enabled = !enabled;
						}
					}
					foreach (IEngine engine in __instance.engines)
					{
						engine.SetInteriorSounds(enabled);
					}
					flag2 = false;
				}
			}
			return flag2;
		}

		private static readonly AccessTools.FieldRef<Aircraft, Renderer[]> cockpitRenderersref = AccessTools.FieldRefAccess<Aircraft, Renderer[]>("cockpitRenderers");

		private static readonly AccessTools.FieldRef<Aircraft, Renderer[]> exteriorRenderersref = AccessTools.FieldRefAccess<Aircraft, Renderer[]>("exteriorRenderers");
	}
}
