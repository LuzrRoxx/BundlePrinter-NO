using System;

namespace Blueprinter
{
	internal readonly struct AddressableKey : IEquatable<AddressableKey>
	{
		public AddressableKey(string guid, string subObjectName, string subObjectType)
		{
			this.Guid = ((guid != null) ? guid.Trim() : null) ?? string.Empty;
			this.SubObjectName = subObjectName ?? string.Empty;
			this.SubObjectType = subObjectType ?? string.Empty;
		}

		public string Guid { get; }

		public string SubObjectName { get; }

		public string SubObjectType { get; }

		public bool HasSubObject
		{
			get
			{
				return !string.IsNullOrEmpty(this.SubObjectName) || !string.IsNullOrEmpty(this.SubObjectType);
			}
		}

		public AddressableKey WithoutSubObject()
		{
			return new AddressableKey(this.Guid, null, null);
		}

		public bool Equals(AddressableKey other)
		{
			return AddressableKey.GuidComparer.Equals(this.Guid, other.Guid) && string.Equals(this.SubObjectName, other.SubObjectName, StringComparison.Ordinal) && string.Equals(this.SubObjectType, other.SubObjectType, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			bool flag;
			if (obj is AddressableKey)
			{
				AddressableKey addressableKey = (AddressableKey)obj;
				flag = this.Equals(addressableKey);
			}
			else
			{
				flag = false;
			}
			return flag;
		}

		public override int GetHashCode()
		{
			int num = AddressableKey.GuidComparer.GetHashCode(this.Guid ?? string.Empty);
			num = (num * 397) ^ ((this.SubObjectName != null) ? StringComparer.Ordinal.GetHashCode(this.SubObjectName) : 0);
			return (num * 397) ^ ((this.SubObjectType != null) ? StringComparer.Ordinal.GetHashCode(this.SubObjectType) : 0);
		}

		private static readonly StringComparer GuidComparer = StringComparer.OrdinalIgnoreCase;
	}
}
