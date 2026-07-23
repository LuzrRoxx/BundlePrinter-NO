using System;
using UnityEngine;

namespace Blueprinter
{
	internal readonly struct AddressableOverrideRecord
	{
		public AddressableOverrideRecord(Object asset, AddressableOverride data, string bundleName)
		{
			this.Asset = asset;
			this.ResourceType = ((asset != null) ? asset.GetType() : typeof(Object));
			this.Guid = ((data != null) ? data.guid : null) ?? string.Empty;
			this.SubObjectName = ((data != null) ? data.subObjectName : null);
			this.SubObjectType = ((data != null) ? data.subObjectType : null);
			this.BundleName = bundleName ?? "<unknown>";
		}

		public Object Asset { get; }

		public Type ResourceType { get; }

		public string Guid { get; }

		public string SubObjectName { get; }

		public string SubObjectType { get; }

		public string BundleName { get; }
	}
}
