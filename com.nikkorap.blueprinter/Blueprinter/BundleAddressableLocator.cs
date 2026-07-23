using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Blueprinter
{
	internal sealed class BundleAddressableLocator : IResourceLocator
	{
		public BundleAddressableLocator(AddressableOverrideRegistry registry, string providerId)
		{
			this._registry = registry;
			this._providerId = providerId;
		}

		public string LocatorId
		{
			get
			{
				return "Blueprinter.BundleAddressableLocator";
			}
		}

		public IEnumerable<object> Keys
		{
			get
			{
				return this._registry.Keys;
			}
		}

		public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
		{
			locations = null;
			AddressableKey addressableKey;
			bool flag = !BundleAddressableLocator.TryExtractKeyData(key, out addressableKey);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				AddressableOverrideRecord addressableOverrideRecord;
				bool flag3 = !this._registry.TryGet(addressableKey.Guid, addressableKey.SubObjectName, addressableKey.SubObjectType, out addressableOverrideRecord);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					Type type2 = addressableOverrideRecord.ResourceType ?? typeof(Object);
					bool flag4 = type != null && type2 != null && !type.IsAssignableFrom(type2);
					if (flag4)
					{
						flag2 = false;
					}
					else
					{
						locations = new List<IResourceLocation>
						{
							new BundleAddressableLocation(addressableKey, addressableOverrideRecord, this._providerId, type2)
						};
						flag2 = true;
					}
				}
			}
			return flag2;
		}

		private static bool TryExtractKeyData(object key, out AddressableKey requestedKey)
		{
			requestedKey = default(AddressableKey);
			string text = key as string;
			if (text == null)
			{
				AssetReference assetReference = key as AssetReference;
				if (assetReference != null)
				{
					bool flag = string.IsNullOrEmpty(assetReference.AssetGUID);
					if (flag)
					{
						return false;
					}
					requestedKey = new AddressableKey(assetReference.AssetGUID, assetReference.SubObjectName, BundleAddressableLocator.GetSubObjectType(assetReference));
					return true;
				}
			}
			else if (!string.IsNullOrEmpty(text))
			{
				requestedKey = new AddressableKey(text, null, null);
				return true;
			}
			return false;
		}

		private static string GetSubObjectType(AssetReference assetReference)
		{
			bool flag = assetReference == null;
			string text;
			if (flag)
			{
				text = null;
			}
			else
			{
				text = BundleAddressableLocator.SubObjectTypeGetter(assetReference);
			}
			return text;
		}

		private static Func<AssetReference, string> CreateSubObjectTypeGetter()
		{
			PropertyInfo property = typeof(AssetReference).GetProperty("subObjectType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			bool flag = property == null || property.PropertyType != typeof(string) || !property.CanRead;
			Func<AssetReference, string> func;
			if (flag)
			{
				func = (AssetReference _) => null;
			}
			else
			{
				func = (AssetReference reference) => property.GetValue(reference) as string;
			}
			return func;
		}

		private readonly AddressableOverrideRegistry _registry;

		private readonly string _providerId;

		private static readonly Func<AssetReference, string> SubObjectTypeGetter = BundleAddressableLocator.CreateSubObjectTypeGetter();
	}
}
