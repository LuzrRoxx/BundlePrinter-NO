using System;

namespace Blueprinter.Ops
{
	[Serializable]
	public class OpAddWeaponMountPayload
	{
		public AssetRef bundleAsset;

		public WeaponManagerTarget[] weaponManagers;
	}
}
