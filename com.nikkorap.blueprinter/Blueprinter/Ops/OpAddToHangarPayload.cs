using System;

namespace Blueprinter.Ops
{
	[Serializable]
	public class OpAddToHangarPayload
	{
		public AssetRef BundleAsset;

		public string[] Hangars;
	}
}
