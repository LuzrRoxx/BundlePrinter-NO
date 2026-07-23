using System;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Blueprinter
{
	internal sealed class BundleAddressableLocation : ResourceLocationBase
	{
		public AddressableKey Key { get; }

		public AddressableOverrideRecord Record { get; }

		public BundleAddressableLocation(AddressableKey key, AddressableOverrideRecord record, string providerId, Type resourceType)
			: base(key.Guid, key.Guid, providerId, resourceType ?? typeof(Object), Array.Empty<IResourceLocation>())
		{
			this.Key = key;
			this.Record = record;
		}
	}
}
