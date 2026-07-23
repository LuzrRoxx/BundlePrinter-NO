using System;
using System.Collections.Generic;

namespace Blueprinter
{
	[Serializable]
	public class AssetPatch
	{
		public LocationRef GameAsset;

		public List<LocationRef> PatchLocations = new List<LocationRef>();
	}
}
