using System;

namespace Blueprinter.Ops
{
	[Serializable]
	public class OpFindAircraftToHangarPayload
	{
		public string HangarKey;

		public string[] AircraftNames;
	}
}
