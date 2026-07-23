using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Blueprinter
{
	public sealed class BundleRegistry
	{
		public IReadOnlyDictionary<string, LoadedBundle> BundlesByName
		{
			get
			{
				return this._bundlesByName;
			}
		}

		public void ScanAndLoad(string pluginLocation)
		{
			string text = Path.GetDirectoryName(pluginLocation);
			bool flag = string.IsNullOrEmpty(text);
			if (flag)
			{
				Plugin.Log.LogError("BundleRegistry: Could not determine root directory from pluginLocation.");
			}
			else
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(text);
				while (directoryInfo != null && !directoryInfo.Name.Equals("plugins", StringComparison.OrdinalIgnoreCase))
				{
					directoryInfo = directoryInfo.Parent;
				}
				string text2;
				if (directoryInfo == null)
				{
					text2 = null;
				}
				else
				{
					DirectoryInfo parent = directoryInfo.Parent;
					text2 = ((parent != null) ? parent.Name : null);
				}
				string text3 = text2;
				bool flag2 = text3 == null || !text3.Equals("BepInEx", StringComparison.OrdinalIgnoreCase);
				if (flag2)
				{
					Plugin.Log.LogError("BundleRegistry: Could not locate BepInEx/plugins.");
				}
				else
				{
					text = directoryInfo.FullName;
					IEnumerable<string> enumerable = Directory.EnumerateFiles(text, "*.nobp", SearchOption.AllDirectories);
					List<string> list = enumerable.OrderByDescending<string, DateTime>(File.GetLastWriteTimeUtc).ThenByDescending<string, DateTime>(File.GetCreationTimeUtc).ThenByDescending<string, string>((string p) => p, StringComparer.OrdinalIgnoreCase).ToList<string>();
					Plugin.Log.LogInfo(string.Format("BundleRegistry: Found {0} .nobp bundle candidate(s) in BepInEx/plugins (newest-first).", list.Count));
					foreach (string text4 in list)
					{
						this.TryAddBundle(text4);
					}
					this.TryLoadEmbeddedBundlesFromPlugins();
				}
			}
		}

		public LoadedBundle GetBundle(string bundleName)
		{
			LoadedBundle loadedBundle;
			this._bundlesByName.TryGetValue(bundleName, out loadedBundle);
			return loadedBundle;
		}

		private void TryAddBundle(string filePath)
		{
			AssetBundle assetBundle = AssetBundle.LoadFromFile(filePath);
			bool flag = assetBundle == null;
			if (flag)
			{
				Plugin.Log.LogWarning("BundleRegistry: Failed to load AssetBundle at '" + filePath + "'");
			}
			else
			{
				bool flag2 = !this.TryRegisterBundle(assetBundle, filePath);
				if (flag2)
				{
					assetBundle.Unload(true);
				}
			}
		}

		private void TryLoadEmbeddedBundlesFromPlugins()
		{
			HashSet<Assembly> hashSet = new HashSet<Assembly>();
			foreach (PluginInfo pluginInfo in Chainloader.PluginInfos.Values)
			{
				Assembly assembly;
				if (pluginInfo == null)
				{
					assembly = null;
				}
				else
				{
					BaseUnityPlugin instance = pluginInfo.Instance;
					assembly = ((instance != null) ? instance.GetType().Assembly : null);
				}
				Assembly assembly2 = assembly;
				bool flag = assembly2 != null && hashSet.Add(assembly2);
				if (flag)
				{
					this.TryLoadEmbeddedBundles(assembly2);
				}
			}
		}

		private void TryLoadEmbeddedBundles(Assembly assembly)
		{
			string[] manifestResourceNames;
			try
			{
				manifestResourceNames = assembly.GetManifestResourceNames();
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError(string.Format("BundleRegistry: Failed to enumerate embedded resources in '{0}': {1}", assembly.FullName, ex));
				return;
			}
			foreach (string text in manifestResourceNames)
			{
				bool flag = !text.EndsWith(".nobp", StringComparison.OrdinalIgnoreCase);
				if (!flag)
				{
					try
					{
						using (Stream manifestResourceStream = assembly.GetManifestResourceStream(text))
						{
							bool flag2 = manifestResourceStream == null;
							if (flag2)
							{
								Plugin.Log.LogWarning(string.Concat(new string[]
								{
									"BundleRegistry: Resource '",
									text,
									"' not found in '",
									assembly.GetName().Name,
									"'."
								}));
							}
							else
							{
								this.TryAddEmbeddedBundle(assembly.GetName().Name + ":" + text, manifestResourceStream);
							}
						}
					}
					catch (Exception ex2)
					{
						Plugin.Log.LogError(string.Format("BundleRegistry: Exception while loading embedded bundle '{0}' from '{1}': {2}", text, assembly.GetName().Name, ex2));
					}
				}
			}
		}

		private void TryAddEmbeddedBundle(string resourceName, Stream resourceStream)
		{
			Plugin.Log.LogDebug("BundleRegistry: Loading embedded bundle resource '" + resourceName + "'");
			AssetBundle assetBundle = null;
			try
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					resourceStream.CopyTo(memoryStream);
					assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
				}
				bool flag = assetBundle == null;
				if (flag)
				{
					Plugin.Log.LogWarning("BundleRegistry: Failed to load AssetBundle from embedded resource '" + resourceName + "'");
				}
				else
				{
					bool flag2 = !this.TryRegisterBundle(assetBundle, "resource:" + resourceName);
					if (flag2)
					{
						assetBundle.Unload(true);
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError(string.Format("BundleRegistry: Exception while loading embedded bundle '{0}': {1}", resourceName, ex));
				if (assetBundle != null)
				{
					assetBundle.Unload(true);
				}
			}
		}

		private bool TryRegisterBundle(AssetBundle bundle, string sourceId)
		{
			bool flag2;
			try
			{
				TextAsset textAsset = bundle.LoadAsset<TextAsset>("patch_manifest");
				bool flag = textAsset == null;
				if (flag)
				{
					Plugin.Log.LogWarning("BundleRegistry: Bundle '" + sourceId + "' has no 'patch_manifest', skipping.");
					flag2 = false;
				}
				else
				{
					PatchManifest patchManifest = JsonUtilities.Deserialize<PatchManifest>(textAsset.text);
					bool flag3 = patchManifest == null || string.IsNullOrEmpty(patchManifest.modName);
					if (flag3)
					{
						Plugin.Log.LogWarning("BundleRegistry: Bundle '" + sourceId + "' has invalid manifest, skipping.");
						flag2 = false;
					}
					else
					{
						Version version = BundleRegistry.SafeParseVersion(patchManifest.modVersion);
						LoadedBundle loadedBundle;
						bool flag4 = this._bundlesByName.TryGetValue(patchManifest.modName, out loadedBundle);
						if (flag4)
						{
							Version version2 = BundleRegistry.SafeParseVersion(loadedBundle.bundleVersion);
							bool flag5 = version <= version2;
							if (flag5)
							{
								Plugin.Log.LogDebug(string.Concat(new string[] { "BundleRegistry: Skipping '", sourceId, "' (v ", patchManifest.modVersion, ") because we already have '", loadedBundle.filePath, "' (v ", loadedBundle.bundleVersion, ")" }));
								return false;
							}
							Plugin.Log.LogDebug(string.Concat(new string[] { "BundleRegistry: Replacing '", loadedBundle.filePath, "' (v ", loadedBundle.bundleVersion, ") with newer '", sourceId, "' (v ", patchManifest.modVersion, ")" }));
							loadedBundle.AssetBundle.Unload(true);
							this._bundlesByName.Remove(patchManifest.modName);
						}
						LoadedBundle loadedBundle2 = new LoadedBundle
						{
							bundleName = patchManifest.modName,
							bundleVersion = patchManifest.modVersion,
							filePath = sourceId,
							AssetBundle = bundle,
							Manifest = patchManifest
						};
						this._bundlesByName.Add(patchManifest.modName, loadedBundle2);
						Plugin.Log.LogDebug(string.Concat(new string[] { "BundleRegistry: Registered bundle '", patchManifest.modName, "' from '", sourceId, "' (v ", patchManifest.modVersion, ")" }));
						flag2 = true;
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError(string.Format("BundleRegistry: Exception while loading '{0}': {1}", sourceId, ex));
				flag2 = false;
			}
			return flag2;
		}

		private static Version SafeParseVersion(string s)
		{
			Version version;
			bool flag = Version.TryParse(s, out version);
			Version version2;
			if (flag)
			{
				version2 = version;
			}
			else
			{
				version2 = new Version(0, 0, 0, 0);
			}
			return version2;
		}

		private readonly Dictionary<string, LoadedBundle> _bundlesByName = new Dictionary<string, LoadedBundle>();

		private const string ManifestAssetName = "patch_manifest";
	}
}
