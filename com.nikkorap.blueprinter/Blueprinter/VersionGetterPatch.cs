using System;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blueprinter
{
	internal static class VersionGetterPatch
	{
		private static string versionString = "";

		[HarmonyPatch(typeof(Application), "version", MethodType.Getter)]
		internal static class ApplicationVersionPatch
		{
			[HarmonyPriority(0)]
			private static void Postfix(ref string __result)
			{
				__result = __result + "_com.nikkorap.blueprinter-v1.8.21_" + Plugin.BundlesHash;
				VersionGetterPatch.versionString = "Nuclear Option-v" + __result.Replace("_", "\n").Replace("--", "    ");
				bool flag = __result.Length > 100;
				if (flag)
				{
					Plugin.Log.LogDebug(string.Format("Version string too long ({0})({1} chars), hashing", __result, __result.Length));
					int num = __result.IndexOf('_');
					string text = ((num >= 0) ? __result.Substring(0, num) : __result);
					using (SHA256 sha = SHA256.Create())
					{
						byte[] array = sha.ComputeHash(Encoding.UTF8.GetBytes(__result));
						string text2 = BitConverter.ToString(array, 0, 6).Replace("-", "").ToLowerInvariant();
						__result = text + "_" + text2;
					}
				}
				Plugin.Log.LogInfo("Updated game version to " + __result);
				bool flag2 = SceneManager.GetActiveScene().name == "MultiplayerMenu";
				if (flag2)
				{
					VersionGetterPatch.VersionDisplayOverlay.SetText(VersionGetterPatch.versionString, null);
				}
			}
		}

		[HarmonyPatch(typeof(SettingsMenu), "Start")]
		internal static class LeaderboardVersionPatch
		{
			private static void Postfix(SettingsMenu __instance)
			{
				VersionGetterPatch.VersionDisplayOverlay.SetText(VersionGetterPatch.versionString, __instance.transform);
			}
		}

		internal sealed class VersionDisplayOverlay : MonoBehaviour
		{
			public static void SetText(string text, Transform parent = null)
			{
				VersionGetterPatch.VersionDisplayOverlay._text = text;
				bool flag = parent == null;
				GameObject gameObject;
				if (flag)
				{
					gameObject = GameObject.Find("__PatchedVersionDisplay");
					bool flag2 = gameObject == null;
					if (flag2)
					{
						gameObject = new GameObject("__PatchedVersionDisplay");
					}
				}
				else
				{
					gameObject = parent.gameObject;
				}
				VersionGetterPatch.VersionDisplayOverlay._instance = gameObject.GetComponent<VersionGetterPatch.VersionDisplayOverlay>() ?? gameObject.AddComponent<VersionGetterPatch.VersionDisplayOverlay>();
			}

			private void OnGUI()
			{
				bool flag = string.IsNullOrEmpty(VersionGetterPatch.VersionDisplayOverlay._text);
				if (!flag)
				{
					GUI.Label(new Rect(10f, 10f, (float)(Screen.width - 20), (float)(Screen.height - 20)), VersionGetterPatch.VersionDisplayOverlay._text);
				}
			}

			private const string ObjectName = "__PatchedVersionDisplay";

			private static VersionGetterPatch.VersionDisplayOverlay _instance;

			private static string _text = "";
		}
	}
}
