using System;
using System.Linq;
using UnityEngine;

namespace Blueprinter.Ops
{
	public sealed class OpAddLoadingScreensHandler : PostOpHandlerBase<OpAddLoadingScreensPayload>
	{
		public override string opId
		{
			get
			{
				return "OpAddLoadingScreens";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpAddLoadingScreensPayload payload)
		{
			Sprite[] array;
			if (payload == null)
			{
				array = null;
			}
			else
			{
				AssetRef[] imagesAssets = payload.imagesAssets;
				array = ((imagesAssets != null) ? imagesAssets.Select<AssetRef, Object>((AssetRef a) => ResourcesAssetResolver.ResolveBundleAsset(bundle, a)).OfType<Sprite>().ToArray<Sprite>() : null);
			}
			Sprite[] array2 = array;
			bool flag = array2 == null || array2.Length == 0;
			if (!flag)
			{
				LoadingScreen loadingScreen = Resources.FindObjectsOfTypeAll<LoadingScreen>().FirstOrDefault<LoadingScreen>();
				bool flag2 = loadingScreen == null;
				if (flag2)
				{
					Plugin.Log.LogWarning("[" + this.opId + "] Prefab 'LoadingScreen' has no LoadingScreen component");
				}
				else
				{
					LoadingScreen loadingScreen2 = loadingScreen;
					Sprite[] images = loadingScreen.images;
					Sprite[] array3 = array2;
					int num = 0;
					Sprite[] array4 = new Sprite[images.Length + array3.Length];
					ReadOnlySpan<Sprite> readOnlySpan = new ReadOnlySpan<Sprite>(images);
					readOnlySpan.CopyTo(new Span<Sprite>(array4).Slice(num, readOnlySpan.Length));
					num += readOnlySpan.Length;
					ReadOnlySpan<Sprite> readOnlySpan2 = new ReadOnlySpan<Sprite>(array3);
					readOnlySpan2.CopyTo(new Span<Sprite>(array4).Slice(num, readOnlySpan2.Length));
					num += readOnlySpan2.Length;
					loadingScreen2.images = array4;
					Plugin.Log.LogDebug(string.Format("[{0}] Added {1} modImages to loading screen pool", this.opId, array2.Length));
				}
			}
		}

		public const string OpId = "OpAddLoadingScreens";
	}
}
