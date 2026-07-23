using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blueprinter.Ops
{
	public sealed class OpAddToEncyclopediaHandler : PostOpHandlerBase<OpAddToEncyclopediaPayload>
	{
		public override string opId
		{
			get
			{
				return "OpAddToEncyclopedia";
			}
		}

		protected override void Handle(LoadedBundle bundle, OpAddToEncyclopediaPayload payload)
		{
			bool flag = ((payload != null) ? payload.entries : null) == null || payload.entries.Length == 0;
			if (flag)
			{
				Plugin.Log.LogWarning("[" + this.opId + "] payload.entries is empty.");
			}
			else
			{
				Plugin instance = Plugin.Instance;
				Encyclopedia encyclopedia = ((instance != null) ? instance._encyclopedia : null);
				bool flag2 = encyclopedia == null;
				if (flag2)
				{
					Plugin.Log.LogWarning("[" + this.opId + "] encyclopedia is null.");
				}
				else
				{
					HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					List<string> list = new List<string>();
					foreach (AssetRef assetRef in payload.entries)
					{
						bool flag3 = assetRef == null;
						if (flag3)
						{
							Plugin.Log.LogWarning("[" + this.opId + "] encountered null AssetRef entry; skipping.");
						}
						else
						{
							string text = string.Concat(new string[] { assetRef.locator, "|", assetRef.name, "|", assetRef.type });
							bool flag4 = !hashSet.Add(text);
							if (flag4)
							{
								Plugin.Log.LogDebug(string.Concat(new string[] { "[", this.opId, "] skipping duplicate entry '", assetRef.name, "'." }));
							}
							else
							{
								Object @object = ResourcesAssetResolver.ResolveBundleAsset(bundle, assetRef);
								bool flag5 = @object == null;
								if (flag5)
								{
									Plugin.Log.LogWarning(string.Concat(new string[] { "[", this.opId, "] could not resolve bundle asset '", assetRef.name, "'." }));
								}
								else
								{
									bool flag6 = !EncyclopediaIncremental.TryAdd(encyclopedia, @object, list);
									if (flag6)
									{
										Plugin.Log.LogWarning(string.Concat(new string[]
										{
											"[",
											this.opId,
											"] unsupported encyclopedia type '",
											@object.GetType().Name,
											"' for '",
											@object.name,
											"'."
										}));
									}
								}
							}
						}
					}
					bool flag7 = list.Count > 0;
					if (!flag7)
					{
						Plugin.Log.LogWarning("[" + this.opId + "] no entries were added to the encyclopedia.");
					}
				}
			}
		}

		public const string OpId = "OpAddToEncyclopedia";
	}
}
