using System;

namespace Blueprinter.Ops
{
	[Serializable]
	public class OpAddMissionsPayload
	{
		public AssetRef[] MissionAssets;

		public string[] MissionGroups;
	}
}
