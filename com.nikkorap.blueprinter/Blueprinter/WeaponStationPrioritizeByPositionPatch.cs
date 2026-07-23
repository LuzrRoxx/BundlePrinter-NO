using System;
using HarmonyLib;
using UnityEngine;

namespace Blueprinter
{
	[HarmonyPatch(typeof(WeaponStation), "PrioritizeByPosition")]
	internal static class WeaponStationPrioritizeByPositionPatch
	{
		private static bool Prefix(Transform transform, Aircraft aircraft, ref float __result)
		{
			Vector3 vector = aircraft.transform.InverseTransformPoint(transform.position);
			__result = vector.x * (vector.x - 0.05f) + Mathf.Abs(vector.y) + Mathf.Abs(vector.z);
			return false;
		}
	}
}
