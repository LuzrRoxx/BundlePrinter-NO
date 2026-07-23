using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mirage;
using UnityEngine;

namespace Blueprinter
{
	internal sealed class PrefabHashAssigner
	{
		public void AssignFromBundles(IReadOnlyDictionary<string, LoadedBundle> bundles)
		{
			bool flag = bundles == null || bundles.Count == 0;
			if (flag)
			{
				Plugin.Log.LogInfo("[PrefabHash] No bundles to scan.");
			}
			else
			{
				HashSet<int> hashSet = new HashSet<int>();
				foreach (NetworkIdentity networkIdentity in Resources.FindObjectsOfTypeAll<NetworkIdentity>())
				{
					bool flag2 = networkIdentity && networkIdentity.PrefabHash != 0;
					if (flag2)
					{
						hashSet.Add(networkIdentity.PrefabHash);
					}
				}
				List<ValueTuple<NetworkIdentity, string>> list = new List<ValueTuple<NetworkIdentity, string>>();
				foreach (LoadedBundle loadedBundle in from kv in bundles.OrderBy<KeyValuePair<string, LoadedBundle>, string>((KeyValuePair<string, LoadedBundle> kv) => kv.Key, StringComparer.Ordinal)
					select kv.Value)
				{
					bool flag3 = ((loadedBundle != null) ? loadedBundle.AssetBundle : null) == null;
					if (!flag3)
					{
						GameObject[] array2;
						try
						{
							array2 = loadedBundle.AssetBundle.LoadAllAssets<GameObject>();
						}
						catch (Exception ex)
						{
							Plugin.Log.LogWarning("[PrefabHash] Failed to load GameObjects from '" + loadedBundle.filePath + "': " + ex.Message);
							continue;
						}
						foreach (GameObject gameObject in array2 ?? Array.Empty<GameObject>())
						{
							bool flag4 = !gameObject;
							if (!flag4)
							{
								foreach (NetworkIdentity networkIdentity2 in gameObject.GetComponentsInChildren<NetworkIdentity>(true))
								{
									bool flag5 = !networkIdentity2;
									if (!flag5)
									{
										list.Add(new ValueTuple<NetworkIdentity, string>(networkIdentity2, PrefabHashAssigner.BuildStableKey(loadedBundle.bundleName, gameObject, networkIdentity2)));
									}
								}
							}
						}
					}
				}
				bool flag6 = list.Count == 0;
				if (flag6)
				{
					Plugin.Log.LogInfo("[PrefabHash] No NetworkIdentities found in bundles.");
				}
				else
				{
					list = list.OrderBy<ValueTuple<NetworkIdentity, string>, string>((ValueTuple<NetworkIdentity, string> c) => c.Item2, StringComparer.Ordinal).ThenBy<ValueTuple<NetworkIdentity, string>, int>((ValueTuple<NetworkIdentity, string> c) => c.Item1.transform.GetSiblingIndex()).ToList<ValueTuple<NetworkIdentity, string>>();
					HashSet<int> hashSet2 = new HashSet<int>(hashSet);
					int num = 0;
					int num2 = 0;
					int num3 = 1;
					foreach (ValueTuple<NetworkIdentity, string> valueTuple in list)
					{
						NetworkIdentity item = valueTuple.Item1;
						int prefabHash = item.PrefabHash;
						bool flag7 = prefabHash != 0 && hashSet2.Add(prefabHash);
						if (flag7)
						{
							num++;
						}
						else
						{
							while (hashSet2.Contains(num3))
							{
								num3++;
							}
							item.PrefabHash = num3;
							hashSet2.Add(num3);
							num2++;
							num3++;
						}
					}
					Plugin.Log.LogInfo(string.Format("[PrefabHash] Processed {0} prefabs (reused={1}, assigned={2}, taken={3}).", new object[] { list.Count, num, num2, hashSet.Count }));
				}
			}
		}

		private static string BuildStableKey(string bundleName, GameObject root, NetworkIdentity ni)
		{
			string text = (string.IsNullOrEmpty(bundleName) ? "<bundle>" : bundleName);
			string text2 = (root ? (root.name ?? "<root>") : "<root>");
			Stack<string> stack = new Stack<string>();
			Transform transform = (ni ? ni.transform : null);
			Transform transform2 = (root ? root.transform : null);
			while (transform != null)
			{
				stack.Push(transform.name);
				bool flag = transform2 != null && transform == transform2;
				if (flag)
				{
					break;
				}
				transform = transform.parent;
			}
			string text3 = string.Join("/", stack);
			return string.Concat(new string[] { text, "|", text2, "|", text3 });
		}
	}
}
