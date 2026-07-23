using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Blueprinter.Ops;
using HarmonyLib;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Blueprinter
{
	[BepInPlugin("com.nikkorap.blueprinter", "Blueprinter", "1.8.21")]
	public class Plugin : BaseUnityPlugin
	{
		public static ManualLogSource Log
		{
			get
			{
				Plugin instance = Plugin.Instance;
				return (instance != null) ? instance.Logger : null;
			}
		}

		public static Plugin Instance { get; private set; }

		public bool PatchingComplete { get; private set; }

		private void Awake()
		{
			Plugin.Instance = this;
			GameObject managerObject = Chainloader.ManagerObject;
			bool flag = managerObject != null;
			if (flag)
			{
				managerObject.hideFlags = HideFlags.HideAndDontSave;
				Object.DontDestroyOnLoad(managerObject);
				Plugin.Log.LogInfo("Force Hid ManagerGameObject");
			}
			this.PatchingComplete = false;
			this.bundleRegistry = new BundleRegistry();
			ResourcesAssetResolver.Initialize();
			this.runner = new PatchRunner(this.bundleRegistry);
			this.runner.RegisterPostOpHandler(new OpAddToHangarHandler());
			this.runner.RegisterPostOpHandler(new OpAddLoadingScreensHandler());
			this.runner.RegisterPostOpHandler(new OpAddWeaponMountToWeaponManagerHandler());
			this.runner.RegisterPostOpHandler(new OpAddToEncyclopediaHandler());
			this.runner.RegisterPostOpHandler(new OpAddMissionsHandler());
			this.runner.RegisterPostOpHandler(new OpFindAircraftToHangarHandler());
			this.addressableRegistry = new AddressableOverrideRegistry();
			this.addressableProvider = new BundleAddressableProvider();
			this.addressableLocator = new BundleAddressableLocator(this.addressableRegistry, "Blueprinter.BundleAddressableProvider");
			this.prefabHashAssigner = new PrefabHashAssigner();
			this.harmony = new Harmony("com.nikkorap.blueprinter");
			this.harmony.PatchAll();
		}

		public static string BuildBundleSignature(IReadOnlyDictionary<string, LoadedBundle> bundles)
		{
			bool flag = bundles == null || bundles.Count == 0;
			string text;
			if (flag)
			{
				text = "NOBUNDLES";
			}
			else
			{
				List<string> list = new List<string>(bundles.Count);
				foreach (KeyValuePair<string, LoadedBundle> keyValuePair in bundles)
				{
					LoadedBundle value = keyValuePair.Value;
					bool flag2 = value == null;
					if (!flag2)
					{
						list.Add("--" + value.bundleName + "-v" + value.bundleVersion);
					}
				}
				list.Sort(StringComparer.Ordinal);
				text = string.Join("_", list);
			}
			return text;
		}

		private static string ShortHash12(string s)
		{
			string text;
			using (SHA1 sha = SHA1.Create())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(s ?? "");
				byte[] array = sha.ComputeHash(bytes);
				StringBuilder stringBuilder = new StringBuilder(12);
				for (int i = 0; i < 6; i++)
				{
					stringBuilder.Append(array[i].ToString("x2"));
				}
				text = stringBuilder.ToString();
			}
			return text;
		}

		private async Task<ValueTuple<bool, GameObject>> SetupAssets()
		{
			MissionKey missionKey = MissionGroup.Default.First();
			MainMenu menu = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault((GameObject go) => go.name == "MainCanvas")?.GetComponentInChildren<MainMenu>();
			if (menu == null)
			{
				return new ValueTuple<bool, GameObject>(false, null);
			}
			while (NetworkManagerNuclearOption.i == null)
			{
				await Task.Delay(10);
			}
			GameObject rewired = null;
			await ResourcesAsyncLoader.LoadPrefab("Rewired", menu.destroyCancellationToken, delegate(GameObject go)
			{
				rewired = go;
			});
			Mission mission;
			string text;
			if (missionKey.TryLoad(out mission, out text))
			{
				MissionManager.SetMission(mission, false);
				await NetworkManagerNuclearOption.i.StartHostAsync(new HostOptions(SocketType.Offline, GameState.SinglePlayer, mission.MapKey));
			}
			return new ValueTuple<bool, GameObject>(true, rewired);
		}

		private async Task FinishSetup(GameObject rewired)
		{
			bool flag = SceneSingleton<GameplayUI>.i != null;
			if (flag)
			{
				SceneSingleton<GameplayUI>.i.ResumeGame();
			}
			await NetworkManagerNuclearOption.i.StopAsync(true);
			Object.DestroyImmediate(rewired);
			Plugin.Log.LogInfo("Setup Complete");
		}

		public IEnumerator RunRoutine()
		{
			BlueprinterLoadingScreen.Create();
			try
			{
				this.bundleRegistry.ScanAndLoad(base.Info.Location);
				Plugin.BundlesHash = Plugin.BuildBundleSignature(this.bundleRegistry.BundlesByName);
				this.prefabHashAssigner.AssignFromBundles(this.bundleRegistry.BundlesByName);
				foreach (KeyValuePair<string, LoadedBundle> kv in this.bundleRegistry.BundlesByName)
				{
					LoadedBundle loadedBundle = kv.Value;
					PatchManifest manifest = loadedBundle.Manifest;
					if (manifest != null && manifest.Patches != null && manifest.Patches.Count != 0)
					{
						int totalLocationsInBundle = 0;
						foreach (AssetPatch patch in manifest.Patches)
						{
							if (patch?.PatchLocations != null)
							{
								totalLocationsInBundle += patch.PatchLocations.Count;
							}
						}
						BlueprinterLoadingScreen.Instance?.SetBundleProgress(manifest.modName, manifest.modVersion, 0, totalLocationsInBundle, false);
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("Fatal during bundle load: " + ex);
				BlueprinterLoadingScreen.DestroyInstance();
			}
			IEnumerator patchEnum = this.runner.ApplyAllPatchesCoroutine((string name, string version, int applied, int total, bool status) => BlueprinterLoadingScreen.Instance?.SetBundleProgress(name, version, applied, total, status));
			for (;;)
			{
				bool moveNext;
				try
				{
					moveNext = patchEnum.MoveNext();
				}
				catch (Exception ex2)
				{
					Plugin.Log.LogError("Fatal during patching: " + ex2);
					break;
				}
				if (!moveNext)
				{
					break;
				}
				yield return patchEnum.Current;
			}
			try
			{
				this.runner.ApplyAllOps();
				this.RegisterAddressableOverrides();
				Plugin.Log.LogInfo("Done.");
			}
			catch (Exception ex3)
			{
				Plugin.Log.LogError("Fatal during finalization: " + ex3);
			}
			this.PatchingComplete = true;
			BlueprinterLoadingScreen.DestroyInstance();
			yield break;
		}

		private void RegisterAddressableOverrides()
		{
			this.addressableRegistry.Clear();
			foreach (KeyValuePair<string, LoadedBundle> keyValuePair in this.bundleRegistry.BundlesByName)
			{
				this.addressableRegistry.RegisterBundle(keyValuePair.Value);
			}
			bool flag = !this.addressableRegistry.HasEntries;
			if (flag)
			{
				Plugin.Log.LogDebug("[Addressables] no bundle overrides declared.");
				bool flag2 = this.addressablesRegistered;
				if (flag2)
				{
					Addressables.RemoveResourceLocator(this.addressableLocator);
					Addressables.ResourceManager.ResourceProviders.Remove(this.addressableProvider);
					this.addressablesRegistered = false;
				}
			}
			else
			{
				bool flag3 = !this.addressablesRegistered;
				if (flag3)
				{
					Addressables.ResourceManager.ResourceProviders.Add(this.addressableProvider);
					Addressables.AddResourceLocator(this.addressableLocator, null, null);
					this.addressablesRegistered = true;
				}
				Plugin.Log.LogDebug(string.Format("[Addressables] registered {0} liveries(s).", this.addressableRegistry.Count));
			}
		}

		public static string BundlesHash = "NOBUNDLES";

		public Encyclopedia _encyclopedia;

		private BundleRegistry bundleRegistry;

		private PatchRunner runner;

		private Harmony harmony;

		private AddressableOverrideRegistry addressableRegistry;

		private BundleAddressableProvider addressableProvider;

		private BundleAddressableLocator addressableLocator;

		private PrefabHashAssigner prefabHashAssigner;

		private bool addressablesRegistered;
	}
}
