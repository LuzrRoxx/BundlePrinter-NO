using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Blueprinter
{
	internal sealed class AddressableOverrideRegistry
	{
		public int Count
		{
			get
			{
				return this._records.Count;
			}
		}

		public bool HasEntries
		{
			get
			{
				return this._records.Count > 0;
			}
		}

		public IEnumerable<object> Keys
		{
			get
			{
				HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (AddressableKey key in this._records.Keys)
				{
					if (!string.IsNullOrEmpty(key.Guid) && seen.Add(key.Guid))
					{
						yield return key.Guid;
					}
				}
			}
		}

		public void Clear()
		{
			this._records.Clear();
		}

		public void RegisterBundle(LoadedBundle bundle)
		{
			bool flag;
			if (bundle == null)
			{
				flag = null != null;
			}
			else
			{
				PatchManifest manifest = bundle.Manifest;
				flag = ((manifest != null) ? manifest.Addressables : null) != null;
			}
			bool flag2 = !flag || bundle.Manifest.Addressables.Count == 0;
			if (!flag2)
			{
				foreach (AddressableOverride addressableOverride in bundle.Manifest.Addressables)
				{
					bool flag3 = ((addressableOverride != null) ? addressableOverride.BundleAsset : null) == null || string.IsNullOrEmpty(addressableOverride.guid);
					if (!flag3)
					{
						Object @object = ResourcesAssetResolver.ResolveBundleAsset(bundle, addressableOverride.BundleAsset);
						bool flag4 = @object == null;
						if (flag4)
						{
							ManualLogSource log = Plugin.Log;
							string[] array = new string[7];
							array[0] = "Addressables: bundle '";
							array[1] = bundle.bundleName;
							array[2] = "' override for GUID '";
							array[3] = addressableOverride.guid;
							array[4] = "' failed to load '";
							int num = 5;
							AssetRef bundleAsset = addressableOverride.BundleAsset;
							string text;
							if ((text = ((bundleAsset != null) ? bundleAsset.locator : null)) == null)
							{
								AssetRef bundleAsset2 = addressableOverride.BundleAsset;
								text = ((bundleAsset2 != null) ? bundleAsset2.name : null);
							}
							array[num] = text;
							array[6] = "'.";
							log.LogWarning(string.Concat(array));
						}
						else
						{
							AddressableKey addressableKey = new AddressableKey(addressableOverride.guid, addressableOverride.subObjectName, addressableOverride.subObjectType);
							this._records[addressableKey] = new AddressableOverrideRecord(@object, addressableOverride, bundle.bundleName);
						}
					}
				}
			}
		}

		public bool TryGet(string guid, string subObjectName, string subObjectType, out AddressableOverrideRecord record)
		{
			record = default(AddressableOverrideRecord);
			bool flag = string.IsNullOrEmpty(guid);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				AddressableKey addressableKey = new AddressableKey(guid, subObjectName, subObjectType);
				bool flag3 = this._records.TryGetValue(addressableKey, out record);
				if (flag3)
				{
					flag2 = true;
				}
				else
				{
					bool flag4 = addressableKey.HasSubObject && this._records.TryGetValue(addressableKey.WithoutSubObject(), out record);
					flag2 = flag4;
				}
			}
			return flag2;
		}

		private readonly Dictionary<AddressableKey, AddressableOverrideRecord> _records = new Dictionary<AddressableKey, AddressableOverrideRecord>();
	}
}
