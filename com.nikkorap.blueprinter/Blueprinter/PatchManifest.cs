using System;
using System.Collections.Generic;

namespace Blueprinter
{
	[Serializable]
	public class PatchManifest
	{
		public string modName;

		public int schemaVersion = 3;

		public string modVersion;

		public List<AssetPatch> Patches = new List<AssetPatch>();

		public List<Op> Ops = new List<Op>();

		public List<AddressableOverride> Addressables = new List<AddressableOverride>();
	}
}
