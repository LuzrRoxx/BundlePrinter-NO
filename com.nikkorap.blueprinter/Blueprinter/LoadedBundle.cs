using System;
using UnityEngine;

namespace Blueprinter
{
	[Serializable]
	public class LoadedBundle
	{
		public string bundleName;

		public string bundleVersion;

		public string filePath;

		public AssetBundle AssetBundle;

		public PatchManifest Manifest;
	}
}
