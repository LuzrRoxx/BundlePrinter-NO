using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Blueprinter.Ops
{
	public sealed class OpFindAircraftToHangarHandler : PostOpHandlerBase<OpFindAircraftToHangarPayload>
	{
		public override string opId
		{
			get
			{
				return "OpFindAircraftToHangar";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpFindAircraftToHangarPayload payload)
		{
			string[] aircraftNames = payload.AircraftNames;
			for (int i = 0; i < aircraftNames.Length; i++)
			{
				string aircraftName = aircraftNames[i];
				AircraftDefinition aircraftDefinition = Resources.FindObjectsOfTypeAll<AircraftDefinition>().FirstOrDefault<AircraftDefinition>((AircraftDefinition x) => x && x.name.Equals(aircraftName, StringComparison.OrdinalIgnoreCase));
				bool flag = aircraftDefinition;
				if (flag)
				{
					OpFindAircraftToHangarHandler._pendingHangarAdds.Add(new ValueTuple<string, AircraftDefinition>(payload.HangarKey, aircraftDefinition));
				}
			}
		}

		public const string OpId = "OpFindAircraftToHangar";

		private static readonly List<(string HangarKey, AircraftDefinition Aircraft)> _pendingHangarAdds = new List<(string HangarKey, AircraftDefinition Aircraft)>();

		[HarmonyPatch(typeof(Airbase), "AddHangar")]
		private static class AirbaseAddHangarPatch
		{
			private static string CleanName(string name)
			{
				return name.Split(new string[] { " (" }, 2, StringSplitOptions.None)[0];
			}

			private static void Prefix(Airbase __instance, Hangar hangar)
			{
				Unit attachedUnit = hangar.attachedUnit;
				string text = OpFindAircraftToHangarHandler.AirbaseAddHangarPatch.CleanName((attachedUnit != null) ? attachedUnit.name : null) + "__" + OpFindAircraftToHangarHandler.AirbaseAddHangarPatch.CleanName(hangar.name);
				foreach (ValueTuple<string, AircraftDefinition> valueTuple in OpFindAircraftToHangarHandler._pendingHangarAdds)
				{
					string item = valueTuple.Item1;
					AircraftDefinition item2 = valueTuple.Item2;
					bool flag = !text.Equals(item, StringComparison.OrdinalIgnoreCase);
					if (!flag)
					{
						bool flag2 = hangar.availableAircraft.Contains(item2);
						if (!flag2)
						{
							hangar.availableAircraft = HarmonyLib.CollectionExtensions.AddToArray<AircraftDefinition>(hangar.availableAircraft, item2);
						}
					}
				}
			}
		}
	}
}
