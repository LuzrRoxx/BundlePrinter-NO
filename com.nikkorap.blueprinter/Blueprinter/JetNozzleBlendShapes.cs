using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Blueprinter
{
	[HarmonyPatch(typeof(JetNozzle), "Thrust")]
	internal static class JetNozzleBlendShapes
	{
		private static void Postfix(JetNozzle __instance, float thrustAmount, float rpmRatio, float thrustRatio, float throttle, bool allowAfterburner)
		{
			bool flag = !__instance;
			if (!flag)
			{
				SkinnedMeshRenderer component;
				bool flag2 = !JetNozzleBlendShapes.Cache.TryGetValue(__instance, out component) || !component || component.gameObject != __instance.gameObject;
				if (flag2)
				{
					component = __instance.GetComponent<SkinnedMeshRenderer>();
					bool flag3 = !component;
					if (flag3)
					{
						return;
					}
					JetNozzleBlendShapes.Cache.Remove(__instance);
					JetNozzleBlendShapes.Cache.Add(__instance, component);
				}
				float num = thrustRatio * 100f;
				float blendShapeWeight = component.GetBlendShapeWeight(0);
				bool flag4 = throttle > 0.9f && allowAfterburner;
				if (flag4)
				{
					float num2 = 1f;
					num = Mathf.Lerp(thrustRatio * 100f, 0f, num2);
				}
				float num3 = ((blendShapeWeight < num) ? Mathf.Min(blendShapeWeight + 1f, num) : Mathf.Max(blendShapeWeight - 1f, num));
				component.SetBlendShapeWeight(0, num3);
			}
		}

		private static readonly ConditionalWeakTable<JetNozzle, SkinnedMeshRenderer> Cache = new ConditionalWeakTable<JetNozzle, SkinnedMeshRenderer>();
	}
}
