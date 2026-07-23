using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Blueprinter.Ops
{
	public sealed class OpAddToHangarHandler : PostOpHandlerBase<OpAddToHangarPayload>
	{
		public override string opId
		{
			get
			{
				return "OpAddToHangar";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpAddToHangarPayload payload)
		{
			bool flag = payload == null || payload.BundleAsset == null || payload.Hangars == null || payload.Hangars.Length == 0;
			if (!flag)
			{
				AircraftDefinition aircraftDefinition = ResourcesAssetResolver.ResolveBundleAsset(bundle, payload.BundleAsset) as AircraftDefinition;
				bool flag2 = !aircraftDefinition;
				if (!flag2)
				{
					foreach (string text in payload.Hangars)
					{
						bool flag3 = string.IsNullOrWhiteSpace(text);
						if (!flag3)
						{
							OpAddToHangarHandler._pendingHangarAdds.Add(new ValueTuple<string, AircraftDefinition>(text, aircraftDefinition));
						}
					}
				}
			}
		}

		public const string OpId = "OpAddToHangar";

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
				bool flag = __instance.name == "airstrip_city2" && hangar.attachedUnit.UniqueName == "<MAP_UNIT>++hangar_med_10";
				if (!flag)
				{
					Unit attachedUnit = hangar.attachedUnit;
					string text = OpAddToHangarHandler.AirbaseAddHangarPatch.CleanName((attachedUnit != null) ? attachedUnit.name : null) + "__" + OpAddToHangarHandler.AirbaseAddHangarPatch.CleanName(hangar.name);
					foreach (ValueTuple<string, AircraftDefinition> valueTuple in OpAddToHangarHandler._pendingHangarAdds)
					{
						string item = valueTuple.Item1;
						AircraftDefinition item2 = valueTuple.Item2;
						bool flag2 = !text.Equals(item, StringComparison.OrdinalIgnoreCase);
						if (!flag2)
						{
							bool flag3 = hangar.availableAircraft.Contains(item2);
							if (!flag3)
							{
								hangar.availableAircraft = HarmonyLib.CollectionExtensions.AddToArray<AircraftDefinition>(hangar.availableAircraft, item2);
							}
						}
					}
				}
			}
		}
	}
}
