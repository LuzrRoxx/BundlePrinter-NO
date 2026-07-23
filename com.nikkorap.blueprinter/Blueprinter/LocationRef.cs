using System;

namespace Blueprinter
{
	[Serializable]
	public class LocationRef
	{
		public string id;

		public AssetRef asset;

		public string hierarchyPath;

		public string componentType;

		public int componentIndex = 0;

		public string memberPath;
	}
}
