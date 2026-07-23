using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Blueprinter.Ops;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace Blueprinter
{
	public class PatchRunner(BundleRegistry bundles)
	{
		public void RegisterPostOpHandler(OpHandlerCore handler)
		{
			bool flag = handler == null;
			if (flag)
			{
				throw new ArgumentNullException("handler");
			}
			this._postOpHandlers[handler.opId] = handler;
		}

		public IEnumerator ApplyAllPatchesCoroutine(Action<string, string, int, int, bool> reportProgress = null)
		{
			foreach (KeyValuePair<string, LoadedBundle> kvp in bundles.BundlesByName)
			{
				LoadedBundle loadedBundle = kvp.Value;
				PatchManifest manifest = loadedBundle.Manifest;
				if (manifest == null || manifest.Patches == null || manifest.Patches.Count == 0)
				{
					continue;
				}
				Plugin.Log.LogDebug("PatchRunner: Applying patches from bundle '" + manifest.modName + "' v" + manifest.modVersion);
				int totalPatchesWithGameAsset = 0;
				int resolvedGameAssets = 0;
				int totalLocationsInBundle = 0;
				foreach (AssetPatch p in manifest.Patches)
				{
					if (p?.PatchLocations != null)
					{
						totalLocationsInBundle += p.PatchLocations.Count;
					}
				}
				int appliedLocationsInBundle = 0;
				int processedLocationsInBundle = 0;
				foreach (AssetPatch patch in manifest.Patches)
				{
					LocationRef gameLocation = patch.GameAsset;
					string patchId = gameLocation.id;
					if (gameLocation == null || gameLocation.asset == null)
					{
						Plugin.Log.LogWarning("PatchRunner: patch '" + patchId + "' has no asset definition.");
						continue;
					}
					List<LocationRef> locations = patch.PatchLocations;
					if (locations == null || locations.Count == 0)
					{
						continue;
					}
					totalPatchesWithGameAsset++;
					Object gameAsset = ResourcesAssetResolver.ResolveGameAsset(gameLocation);
					if (gameAsset == null)
					{
						Plugin.Log.LogWarning("PatchRunner: patch '" + patchId + "' could not resolve base-game source asset. Skipping its locations.");
						continue;
					}
					resolvedGameAssets++;
					foreach (LocationRef loc in locations)
					{
						processedLocationsInBundle++;
						bool ok = this.ApplySingleLocation(loadedBundle, patchId, gameAsset, loc);
						if (ok)
						{
							appliedLocationsInBundle++;
						}
						if (reportProgress != null)
						{
							reportProgress(manifest.modName, manifest.modVersion, appliedLocationsInBundle, totalLocationsInBundle, !ok);
						}
						if (processedLocationsInBundle % 1024 == 0)
						{
							yield return null;
						}
					}
				}
				string msg = string.Concat(new string[]
				{
					"PatchRunner: Bundle report: ",
					manifest.modName,
					" v ",
					manifest.modVersion,
					"\n",
					string.Format("        {0}/{1} GameAssets found and resolved\n", resolvedGameAssets, totalPatchesWithGameAsset),
					string.Format("        {0}/{1} patches applied.", appliedLocationsInBundle, totalLocationsInBundle)
				});
				bool success = totalLocationsInBundle == appliedLocationsInBundle && totalPatchesWithGameAsset == resolvedGameAssets;
				if (success)
				{
					Plugin.Log.LogInfo(msg);
				}
				else
				{
					Plugin.Log.LogWarning(msg);
				}
				if (reportProgress != null)
				{
					reportProgress(manifest.modName, manifest.modVersion, appliedLocationsInBundle, totalLocationsInBundle, false);
				}
				yield return null;
			}
			yield break;
		}

		public void ApplyAllOps()
		{
			foreach (KeyValuePair<string, LoadedBundle> keyValuePair in bundles.BundlesByName)
			{
				LoadedBundle value = keyValuePair.Value;
				PatchManifest manifest = value.Manifest;
				bool flag = manifest == null || manifest.Ops == null || manifest.Ops.Count == 0;
				if (!flag)
				{
					Plugin.Log.LogDebug(string.Format("PatchRunner: Applying {0} ops from bundle '{1}' v{2}", manifest.Ops.Count, manifest.modName, manifest.modVersion));
					foreach (Op op in manifest.Ops)
					{
						this.ApplySingleOp(value, manifest, op);
					}
				}
			}
		}

		private void ApplySingleOp(LoadedBundle bundle, PatchManifest manifest, Op op)
		{
			bool flag = op == null;
			if (!flag)
			{
				OpHandlerCore opHandlerCore;
				bool flag2 = !this._postOpHandlers.TryGetValue(op.opId, out opHandlerCore);
				if (flag2)
				{
					Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: no handler registered for op '", op.opId, "' in bundle '", manifest.modName, "'." }));
				}
				else
				{
					try
					{
						opHandlerCore.Execute(bundle, op.payloadJson);
					}
					catch (Exception ex)
					{
						Plugin.Log.LogError(string.Format("PatchRunner: exception while executing op '{0}' in bundle '{1}': {2}", op.opId, manifest.modName, ex));
					}
				}
			}
		}

		private bool ApplySingleLocation(LoadedBundle loadedBundle, string patchId, Object gameAsset, LocationRef loc)
		{
			bool flag = loc == null;
			bool flag2;
			if (flag)
			{
				Plugin.Log.LogWarning("PatchRunner: encountered null patch location in patch '" + patchId + "'.");
				flag2 = false;
			}
			else
			{
				string id = loc.id;
				bool flag3 = loc.asset == null;
				if (flag3)
				{
					Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: location '", id, "' in patch '", patchId, "' has no asset reference." }));
					flag2 = false;
				}
				else
				{
					Object @object = ResourcesAssetResolver.ResolveBundleAsset(loadedBundle, loc.asset);
					bool flag4 = @object == null;
					if (flag4)
					{
						Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: location '", id, "' in patch '", patchId, "': could not resolve bundle target asset." }));
						flag2 = false;
					}
					else
					{
						Object object2 = ResourcesAssetResolver.ResolveBundleTargetObject(@object, loc);
						bool flag5 = object2 == null;
						if (flag5)
						{
							Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: location '", id, "' in patch '", patchId, "': could not resolve target object." }));
							flag2 = false;
						}
						else
						{
							bool flag6 = string.IsNullOrEmpty(loc.memberPath);
							if (flag6)
							{
								Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: location '", id, "' in patch '", patchId, "' has empty memberPath." }));
								flag2 = false;
							}
							else
							{
								string text = loc.memberPath;
								try
								{
									bool flag7 = this.TryApplyCameraRendererIndexPatch(object2, gameAsset, text, patchId, id);
									if (flag7)
									{
										flag2 = true;
									}
									else
									{
										AudioMixer audioMixer = gameAsset as AudioMixer;
										bool flag8 = audioMixer != null && text.StartsWith("outputAudioMixerGroup", StringComparison.Ordinal);
										if (flag8)
										{
											string text2 = null;
											int num = text.IndexOf("::", StringComparison.Ordinal);
											bool flag9 = num >= 0;
											if (flag9)
											{
												string text3 = text;
												int num2 = num + "::".Length;
												text2 = text3.Substring(num2, text3.Length - num2);
												text = text.Substring(0, num);
											}
											bool flag10 = !string.IsNullOrEmpty(text2);
											if (flag10)
											{
												AudioMixerGroup[] array = audioMixer.FindMatchingGroups(text2);
												bool flag11 = array == null || array.Length == 0;
												if (flag11)
												{
													Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", id, "': AudioMixer '", audioMixer.name, "' has no group matching '", text2, "'." }));
													return false;
												}
												AudioMixerGroup audioMixerGroup = array[0];
												MemberPathSetter.Apply(object2, text, audioMixerGroup);
												return true;
											}
										}
										bool flag12 = this.TryApplyRendererMaterialArrayPatch(object2, gameAsset, text, patchId, id);
										if (flag12)
										{
											flag2 = true;
										}
										else
										{
											MemberPathSetter.Apply(object2, text, gameAsset);
											flag2 = true;
										}
									}
								}
								catch (Exception ex)
								{
									Plugin.Log.LogError(string.Format("PatchRunner: exception applying patch '{0}' at '{1}' ({2}): {3}", new object[] { patchId, id, loc.memberPath, ex }));
									flag2 = false;
								}
							}
						}
					}
				}
			}
			return flag2;
		}

		private bool TryApplyCameraRendererIndexPatch(Object bundleTargetObject, Object gameAsset, string memberPath, string patchId, string locationId)
		{
			bool flag = bundleTargetObject == null || gameAsset == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				bool flag3 = !string.Equals(memberPath, "rendererIndex", StringComparison.Ordinal) && !string.Equals(memberPath, "m_RendererIndex", StringComparison.Ordinal);
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					ScriptableObject currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
					bool flag4 = currentRenderPipeline == null;
					if (flag4)
					{
						Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", locationId, "': no currentRenderPipeline, cannot resolve rendererIndex." }));
						flag2 = false;
					}
					else
					{
						Type type = currentRenderPipeline.GetType();
						FieldInfo fieldInfo = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic) ?? type.GetField("m_RendererData", BindingFlags.Instance | BindingFlags.NonPublic);
						bool flag5 = fieldInfo == null;
						if (flag5)
						{
							Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", locationId, "': pipeline asset '", currentRenderPipeline.name, "' has no renderer list field." }));
							flag2 = false;
						}
						else
						{
							object value = fieldInfo.GetValue(currentRenderPipeline);
							Array array = value as Array;
							bool flag6 = array == null;
							if (flag6)
							{
								Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", locationId, "': renderer list field on '", currentRenderPipeline.name, "' is not an array." }));
								flag2 = false;
							}
							else
							{
								bool flag7 = gameAsset == null;
								if (flag7)
								{
									flag2 = false;
								}
								else
								{
									int num = -1;
									for (int i = 0; i < array.Length; i++)
									{
										Object @object = array.GetValue(i) as Object;
										bool flag8 = @object == gameAsset;
										if (flag8)
										{
											num = i;
											break;
										}
									}
									bool flag9 = num < 0;
									if (flag9)
									{
										Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", locationId, "': renderer '", gameAsset.name, "' not found in pipeline '", currentRenderPipeline.name, "'." }));
										flag2 = false;
									}
									else
									{
										try
										{
											Traverse traverse = Traverse.Create(bundleTargetObject);
											Traverse traverse2 = traverse.Field("m_RendererIndex");
											bool flag10 = !traverse2.FieldExists();
											if (flag10)
											{
												Plugin.Log.LogWarning(string.Concat(new string[]
												{
													"PatchRunner: patch '",
													patchId,
													"' at '",
													locationId,
													"': field 'm_RendererIndex' not found on ",
													bundleTargetObject.GetType().FullName,
													"."
												}));
												flag2 = false;
											}
											else
											{
												traverse2.SetValue(num);
												flag2 = true;
											}
										}
										catch (Exception ex)
										{
											Plugin.Log.LogError(string.Format("PatchRunner: patch '{0}' at '{1}': exception while setting m_RendererIndex: {2}", patchId, locationId, ex));
											flag2 = false;
										}
									}
								}
							}
						}
					}
				}
			}
			return flag2;
		}

		private bool TryApplyRendererMaterialArrayPatch(Object target, Object gameAsset, string memberPath, string patchId, string LocationId)
		{
			Renderer renderer = target as Renderer;
			bool flag = renderer == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				Material material = gameAsset as Material;
				bool flag3 = material == null;
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = string.IsNullOrEmpty(memberPath);
					if (flag4)
					{
						flag2 = false;
					}
					else
					{
						bool flag5 = memberPath.StartsWith("sharedMaterials[", StringComparison.Ordinal);
						bool flag6;
						string text;
						if (flag5)
						{
							flag6 = true;
							text = "sharedMaterials[";
						}
						else
						{
							bool flag7 = memberPath.StartsWith("materials[", StringComparison.Ordinal);
							if (!flag7)
							{
								return false;
							}
							flag6 = false;
							text = "materials[";
						}
						int num = memberPath.IndexOf(']', text.Length);
						bool flag8 = num < 0;
						if (flag8)
						{
							Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", LocationId, "': malformed material memberPath '", memberPath, "'." }));
							flag2 = false;
						}
						else
						{
							int length = text.Length;
							string text2 = memberPath.Substring(length, num - length);
							int num2;
							bool flag9 = !int.TryParse(text2, out num2) || num2 < 0;
							if (flag9)
							{
								Plugin.Log.LogWarning(string.Concat(new string[] { "PatchRunner: patch '", patchId, "' at '", LocationId, "': invalid material index '", text2, "' in '", memberPath, "'." }));
								flag2 = false;
							}
							else
							{
								try
								{
									Material[] array = (flag6 ? renderer.sharedMaterials : renderer.materials);
									int num3 = ((array != null) ? array.Length : 0);
									bool flag10 = array == null || num3 == 0 || num2 >= num3;
									if (flag10)
									{
										Plugin.Log.LogWarning(string.Format("PatchRunner: patch '{0}' at '{1}': material index {2} out of range (len={3}) on Renderer '{4}'.", new object[]
										{
											patchId,
											LocationId,
											num2,
											num3,
											renderer.gameObject.name
										}));
										flag2 = false;
									}
									else
									{
										array[num2] = material;
										bool flag11 = flag6;
										if (flag11)
										{
											renderer.sharedMaterials = array;
										}
										else
										{
											renderer.materials = array;
										}
										flag2 = true;
									}
								}
								catch (Exception ex)
								{
									Plugin.Log.LogError(string.Format("PatchRunner: patch '{0}' at '{1}': exception while assigning material via '{2}': {3}", new object[] { patchId, LocationId, memberPath, ex }));
									flag2 = false;
								}
							}
						}
					}
				}
			}
			return flag2;
		}

		private readonly Dictionary<string, OpHandlerCore> _postOpHandlers = new Dictionary<string, OpHandlerCore>(StringComparer.Ordinal);
	}
}
