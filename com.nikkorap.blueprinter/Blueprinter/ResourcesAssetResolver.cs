using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Blueprinter
{
	public static class ResourcesAssetResolver
	{
		public static void Initialize()
		{
			ResourcesAssetResolver._baseCache.Clear();
		}

		public static Object ResolveGameAsset(LocationRef location)
		{
			bool flag = location == null;
			Object @object;
			if (flag)
			{
				@object = null;
			}
			else
			{
				bool flag2 = location.asset == null;
				if (flag2)
				{
					ManualLogSource log = ResourcesAssetResolver.Log;
					if (log != null)
					{
						log.LogWarning("ResolveGameAsset(LocationRef): location has no asset reference.");
					}
					@object = null;
				}
				else
				{
					Object object2 = ResourcesAssetResolver.ResolveGameAsset(location.asset);
					bool flag3 = object2 == null;
					if (flag3)
					{
						@object = null;
					}
					else
					{
						bool flag4 = string.IsNullOrEmpty(location.hierarchyPath) && string.IsNullOrEmpty(location.componentType);
						if (flag4)
						{
							@object = object2;
						}
						else
						{
							@object = ResourcesAssetResolver.ResolveBundleTargetObject(object2, location);
						}
					}
				}
			}
			return @object;
		}

		public static Object ResolveGameAsset(AssetRef assetRef)
		{
			bool flag = assetRef == null;
			Object @object;
			if (flag)
			{
				@object = null;
			}
			else
			{
				Type type = ResourcesAssetResolver.ResolveType(assetRef.type);
				bool flag2 = type == null;
				if (flag2)
				{
					ManualLogSource log = ResourcesAssetResolver.Log;
					if (log != null)
					{
						log.LogWarning("ResolveGameAsset: could not resolve type '" + assetRef.type + "'.");
					}
					@object = null;
				}
				else
				{
					string text = assetRef.name ?? assetRef.locator;
					bool flag3 = string.IsNullOrEmpty(text);
					if (flag3)
					{
						ManualLogSource log2 = ResourcesAssetResolver.Log;
						if (log2 != null)
						{
							log2.LogWarning("ResolveGameAsset: assetRef for type '" + assetRef.type + "' has no name/locator.");
						}
						@object = null;
					}
					else
					{
						Object object2;
						bool flag4 = ResourcesAssetResolver._baseCache.TryGetValue(new ValueTuple<Type, string>(type, text), out object2);
						if (flag4)
						{
							@object = object2;
						}
						else
						{
							Object[] array = Resources.FindObjectsOfTypeAll(type);
							Object object3 = null;
							foreach (Object object4 in array)
							{
								bool flag5 = object4 == null;
								if (!flag5)
								{
									GameObject gameObject = object4 as GameObject;
									bool flag6 = gameObject != null && gameObject.transform.parent != null;
									if (!flag6)
									{
										bool flag7 = string.Equals(object4.name, text, StringComparison.OrdinalIgnoreCase);
										if (flag7)
										{
											object3 = object4;
											break;
										}
										Component component = object4 as Component;
										bool flag8 = component != null;
										if (flag8)
										{
											Transform transform = component.transform;
											string text2;
											if (transform == null)
											{
												text2 = null;
											}
											else
											{
												Transform root = transform.root;
												text2 = ((root != null) ? root.name : null);
											}
											string text3 = text2;
											bool flag9 = !string.IsNullOrEmpty(text3) && string.Equals(text3, text, StringComparison.OrdinalIgnoreCase);
											if (flag9)
											{
												object3 = object4;
												break;
											}
										}
									}
								}
							}
							bool flag10 = object3 == null;
							if (flag10)
							{
								ManualLogSource log3 = ResourcesAssetResolver.Log;
								if (log3 != null)
								{
									log3.LogWarning(string.Concat(new string[] { "ResolveGameAsset: could not find base-game asset '", text, "' of type '", assetRef.type, "' via Resources.FindObjectsOfTypeAll." }));
								}
								@object = null;
							}
							else
							{
								ResourcesAssetResolver._baseCache[new ValueTuple<Type, string>(type, text)] = object3;
								@object = object3;
							}
						}
					}
				}
			}
			return @object;
		}

		public static Object ResolveBundleAsset(LoadedBundle bundle, AssetRef targetRef)
		{
			bool flag = bundle == null || bundle.AssetBundle == null || targetRef == null;
			Object @object;
			if (flag)
			{
				@object = null;
			}
			else
			{
				Type type = ResourcesAssetResolver.ResolveType(targetRef.type);
				string text = targetRef.locator ?? targetRef.name;
				bool flag2 = string.IsNullOrEmpty(text);
				if (flag2)
				{
					ManualLogSource log = ResourcesAssetResolver.Log;
					if (log != null)
					{
						log.LogWarning("ResolveBundleAsset: targetRef for bundle '" + bundle.bundleName + "' has no locator/name.");
					}
					@object = null;
				}
				else
				{
					try
					{
						ValueTuple<AssetBundle, string, Type> valueTuple = new ValueTuple<AssetBundle, string, Type>(bundle.AssetBundle, text, type);
						Object object2;
						bool flag3 = ResourcesAssetResolver._bundleAssetCache.TryGetValue(valueTuple, out object2);
						if (flag3)
						{
							@object = object2;
						}
						else
						{
							bool flag4 = type != null;
							Object object3;
							if (flag4)
							{
								object3 = bundle.AssetBundle.LoadAsset(text, type);
							}
							else
							{
								object3 = bundle.AssetBundle.LoadAsset(text);
							}
							bool flag5 = object3 == null;
							if (flag5)
							{
								ManualLogSource log2 = ResourcesAssetResolver.Log;
								if (log2 != null)
								{
									log2.LogWarning(string.Concat(new string[] { "ResolveBundleAsset: could not load '", text, "' (type '", targetRef.type, "') from bundle '", bundle.bundleName, "'." }));
								}
							}
							else
							{
								ResourcesAssetResolver.NormalizeBundleAssetShaders(object3, bundle.bundleName, text);
							}
							ResourcesAssetResolver._bundleAssetCache[valueTuple] = object3;
							@object = object3;
						}
					}
					catch (Exception ex)
					{
						ManualLogSource log3 = ResourcesAssetResolver.Log;
						if (log3 != null)
						{
							log3.LogError(string.Format("ResolveBundleAsset: exception while loading '{0}' from bundle '{1}': {2}", text, bundle.filePath, ex));
						}
						@object = null;
					}
				}
			}
			return @object;
		}

		public static Object ResolveBundleTargetObject(Object targetAsset, LocationRef loc)
		{
			bool flag = targetAsset == null || loc == null;
			Object @object;
			if (flag)
			{
				@object = null;
			}
			else
			{
				bool flag2 = string.IsNullOrEmpty(loc.hierarchyPath) && string.IsNullOrEmpty(loc.componentType);
				if (flag2)
				{
					@object = targetAsset;
				}
				else
				{
					GameObject gameObject = targetAsset as GameObject;
					bool flag3 = gameObject != null;
					GameObject gameObject2;
					if (flag3)
					{
						gameObject2 = gameObject;
					}
					else
					{
						Component component = targetAsset as Component;
						bool flag4 = component != null;
						if (!flag4)
						{
							ManualLogSource log = ResourcesAssetResolver.Log;
							if (log != null)
							{
								log.LogWarning("ResolveBundleTargetObject: targetAsset '" + targetAsset.name + "' is not a GameObject/Component, but hierarchy/component info was provided. Falling back to the asset itself.");
							}
							return targetAsset;
						}
						gameObject2 = component.gameObject;
					}
					Transform transform = gameObject2.transform;
					bool flag5 = !string.IsNullOrEmpty(loc.hierarchyPath);
					if (flag5)
					{
						Transform transform2 = gameObject2.transform.Find(loc.hierarchyPath);
						bool flag6 = transform2 == null;
						if (flag6)
						{
							ManualLogSource log2 = ResourcesAssetResolver.Log;
							if (log2 != null)
							{
								log2.LogWarning(string.Concat(new string[] { "ResolveBundleTargetObject: hierarchy path '", loc.hierarchyPath, "' not found under '", gameObject2.name, "'." }));
							}
							return null;
						}
						transform = transform2;
					}
					bool flag7 = string.IsNullOrEmpty(loc.componentType);
					if (flag7)
					{
						@object = transform.gameObject;
					}
					else
					{
						Type type = ResourcesAssetResolver.ResolveType(loc.componentType);
						bool flag8 = type == null;
						if (flag8)
						{
							ManualLogSource log3 = ResourcesAssetResolver.Log;
							if (log3 != null)
							{
								log3.LogWarning("ResolveBundleTargetObject: could not resolve component type '" + loc.componentType + "'.");
							}
							@object = null;
						}
						else
						{
							Component[] components = transform.GetComponents(type);
							bool flag9 = components == null || components.Length == 0;
							if (flag9)
							{
								ManualLogSource log4 = ResourcesAssetResolver.Log;
								if (log4 != null)
								{
									log4.LogWarning(string.Concat(new string[]
									{
										"ResolveBundleTargetObject: component '",
										loc.componentType,
										"' not found on '",
										transform.gameObject.name,
										"'."
									}));
								}
								@object = null;
							}
							else
							{
								int componentIndex = loc.componentIndex;
								bool flag10 = componentIndex < 0 || componentIndex >= components.Length;
								if (flag10)
								{
									ManualLogSource log5 = ResourcesAssetResolver.Log;
									if (log5 != null)
									{
										log5.LogWarning(string.Format("ResolveBundleTargetObject: requested index {0} for component '{1}' on '{2}', ", componentIndex, loc.componentType, transform.gameObject.name) + string.Format("but only found {0}.", components.Length));
									}
									@object = null;
								}
								else
								{
									@object = components[componentIndex];
								}
							}
						}
					}
				}
			}
			return @object;
		}

		private static ManualLogSource Log
		{
			get
			{
				return Plugin.Log;
			}
		}

		private static Type ResolveType(string typeName)
		{
			bool flag = string.IsNullOrEmpty(typeName);
			Type type;
			if (flag)
			{
				type = null;
			}
			else
			{
				Type type2 = Type.GetType(typeName);
				bool flag2 = type2 != null;
				if (flag2)
				{
					type = type2;
				}
				else
				{
					type2 = typeof(GameObject).Assembly.GetType(typeName);
					bool flag3 = type2 != null;
					if (flag3)
					{
						type = type2;
					}
					else
					{
						type2 = typeof(ScriptableObject).Assembly.GetType(typeName);
						type = type2;
					}
				}
			}
			return type;
		}

		private static void NormalizeBundleAssetShaders(Object asset, string bundleName, string assetKey)
		{
			try
			{
				int num = 0;
				Material material = asset as Material;
				if (material == null)
				{
					GameObject gameObject = asset as GameObject;
					if (gameObject == null)
					{
						Renderer renderer = asset as Renderer;
						if (renderer == null)
						{
							Component component = asset as Component;
							if (component != null)
							{
								foreach (Renderer renderer2 in component.GetComponentsInChildren<Renderer>(true))
								{
									num += ResourcesAssetResolver.NormalizeRendererMaterials(renderer2);
								}
							}
						}
						else
						{
							num += ResourcesAssetResolver.NormalizeRendererMaterials(renderer);
						}
					}
					else
					{
						foreach (Renderer renderer3 in gameObject.GetComponentsInChildren<Renderer>(true))
						{
							num += ResourcesAssetResolver.NormalizeRendererMaterials(renderer3);
						}
					}
				}
				else
				{
					bool flag = ResourcesAssetResolver.NormalizeMaterialShader(material);
					if (flag)
					{
						num++;
					}
				}
				bool flag2 = num > 0;
				if (flag2)
				{
					ManualLogSource log = ResourcesAssetResolver.Log;
					if (log != null)
					{
						log.LogDebug(string.Concat(new string[]
						{
							string.Format("ResolveBundleAsset: rebound {0} duplicate shader reference(s) ", num),
							"in '",
							assetKey,
							"' from bundle '",
							bundleName,
							"'."
						}));
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log2 = ResourcesAssetResolver.Log;
				if (log2 != null)
				{
					log2.LogWarning("ResolveBundleAsset: failed to normalize shaders for '" + assetKey + "' " + string.Format("from bundle '{0}': {1}", bundleName, ex));
				}
			}
		}

		private static int NormalizeRendererMaterials(Renderer renderer)
		{
			bool flag = renderer == null;
			int num;
			if (flag)
			{
				num = 0;
			}
			else
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				bool flag2 = sharedMaterials == null || sharedMaterials.Length == 0;
				if (flag2)
				{
					num = 0;
				}
				else
				{
					int num2 = 0;
					foreach (Material material in sharedMaterials)
					{
						bool flag3 = ResourcesAssetResolver.NormalizeMaterialShader(material);
						if (flag3)
						{
							num2++;
						}
					}
					num = num2;
				}
			}
			return num;
		}

		private static bool NormalizeMaterialShader(Material material)
		{
			bool flag = material == null || material.shader == null;
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				Shader shader = material.shader;
				string name = shader.name;
				Shader shader2 = Shader.Find(name);
				bool flag3 = shader2 == null;
				if (flag3)
				{
					flag2 = false;
				}
				else
				{
					bool flag4 = shader == shader2;
					if (flag4)
					{
						flag2 = false;
					}
					else
					{
						string[] shaderKeywords = material.shaderKeywords;
						int rawRenderQueue = material.rawRenderQueue;
						MaterialGlobalIlluminationFlags globalIlluminationFlags = material.globalIlluminationFlags;
						bool enableInstancing = material.enableInstancing;
						bool doubleSidedGI = material.doubleSidedGI;
						material.shader = shader2;
						material.shaderKeywords = shaderKeywords;
						material.renderQueue = rawRenderQueue;
						material.globalIlluminationFlags = globalIlluminationFlags;
						material.enableInstancing = enableInstancing;
						material.doubleSidedGI = doubleSidedGI;
						ManualLogSource log = ResourcesAssetResolver.Log;
						if (log != null)
						{
							log.LogDebug("Rebound duplicate shader on material '" + material.name + "': " + string.Format("'{0}' {1} -> {2}", name, shader.GetInstanceID(), shader2.GetInstanceID()));
						}
						flag2 = true;
					}
				}
			}
			return flag2;
		}

		private static readonly Dictionary<ValueTuple<Type, string>, Object> _baseCache = new Dictionary<ValueTuple<Type, string>, Object>();

		private static readonly Dictionary<ValueTuple<AssetBundle, string, Type>, Object> _bundleAssetCache = new Dictionary<ValueTuple<AssetBundle, string, Type>, Object>();
	}
}
