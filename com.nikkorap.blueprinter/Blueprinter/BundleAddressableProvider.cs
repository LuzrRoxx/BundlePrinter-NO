using System;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Blueprinter
{
	internal sealed class BundleAddressableProvider : IResourceProvider
	{
		public string ProviderId
		{
			get
			{
				return "Blueprinter.BundleAddressableProvider";
			}
		}

		ProviderBehaviourFlags IResourceProvider.BehaviourFlags
		{
			get
			{
				return ProviderBehaviourFlags.None;
			}
		}

		public Type GetDefaultType(IResourceLocation location)
		{
			BundleAddressableLocation bundleAddressableLocation = location as BundleAddressableLocation;
			bool flag = bundleAddressableLocation != null;
			Type type;
			if (flag)
			{
				type = bundleAddressableLocation.Record.ResourceType ?? typeof(Object);
			}
			else
			{
				type = typeof(Object);
			}
			return type;
		}

		public bool CanProvide(Type type, IResourceLocation location)
		{
			BundleAddressableLocation bundleAddressableLocation = location as BundleAddressableLocation;
			bool flag = bundleAddressableLocation == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				Type type2 = bundleAddressableLocation.Record.ResourceType ?? typeof(Object);
				flag2 = type == null || type2 == null || type.IsAssignableFrom(type2);
			}
			return flag2;
		}

		public void Provide(ProvideHandle handle)
		{
			BundleAddressableLocation bundleAddressableLocation = handle.Location as BundleAddressableLocation;
			bool flag = bundleAddressableLocation == null;
			if (flag)
			{
				handle.Complete<Object>(null, false, new InvalidOperationException("BundleAddressableProvider received an unknown location."));
			}
			else
			{
				Object asset = bundleAddressableLocation.Record.Asset;
				Type type = handle.Type;
				bool flag2 = asset != null && type != null && !type.IsInstanceOfType(asset);
				if (flag2)
				{
					InvalidOperationException ex = new InvalidOperationException(string.Format("Addressables override '{0}' expected type '{1}' but loaded '{2}'.", bundleAddressableLocation.Record.Guid, type, asset.GetType()));
					Plugin.Log.LogWarning(ex.Message);
					handle.Complete<Object>(null, false, ex);
				}
				else
				{
					bool flag3 = asset == null;
					if (flag3)
					{
						InvalidOperationException ex2 = new InvalidOperationException(string.Concat(new string[]
						{
							"Addressables override '",
							bundleAddressableLocation.Record.Guid,
							"' in bundle '",
							bundleAddressableLocation.Record.BundleName,
							"' did not cache an asset."
						}));
						Plugin.Log.LogWarning(ex2.Message);
						handle.Complete<Object>(null, false, ex2);
					}
					else
					{
						handle.Complete<Object>(asset, true, null);
					}
				}
			}
		}

		public void Release(IResourceLocation location, object obj)
		{
		}

		public const string Id = "Blueprinter.BundleAddressableProvider";
	}
}
