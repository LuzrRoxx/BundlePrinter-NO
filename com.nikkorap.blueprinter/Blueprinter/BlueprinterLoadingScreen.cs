using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Blueprinter
{
	public class BlueprinterLoadingScreen : MonoBehaviour
	{
		public static BlueprinterLoadingScreen Instance { get; private set; }

		private bool notDedicatedServer
		{
			get
			{
				return !Application.isBatchMode && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
			}
		}

		public static void Create()
		{
			bool flag = BlueprinterLoadingScreen.Instance != null;
			if (!flag)
			{
				GameObject gameObject = new GameObject("BlueprinterLoadingScreen");
				BlueprinterLoadingScreen.Instance = gameObject.AddComponent<BlueprinterLoadingScreen>();
				Object.DontDestroyOnLoad(gameObject);
				BlueprinterLoadingScreen.Instance.EnsureUniversalUI();
			}
		}

		public static void DestroyInstance()
		{
			bool flag = BlueprinterLoadingScreen.Instance == null;
			if (!flag)
			{
				BlueprinterLoadingScreen.Instance.MarkDoneAndTryClose();
				Object.Destroy(BlueprinterLoadingScreen.Instance.gameObject);
				BlueprinterLoadingScreen.Instance = null;
			}
		}

		public void SetBundleProgress(string bundleName, string bundleVer, int current, int total, bool ErrorFound = false)
		{
			bool flag = !this.notDedicatedServer;
			if (!flag)
			{
				this.EnsureUniversalUI();
				bool flag2 = !this.barContainer;
				if (!flag2)
				{
					string text = bundleName + "\n" + bundleVer;
					BlueprinterLoadingScreen.Bar orCreateBar = this.GetOrCreateBar(text);
					if (ErrorFound)
					{
						orCreateBar.HasError = true;
					}
					total = Mathf.Max(1, total);
					current = Mathf.Clamp(current, 0, total);
					orCreateBar.BundleName = bundleName ?? "";
					orCreateBar.BundleVer = bundleVer ?? "";
					orCreateBar.Current = current;
					orCreateBar.Total = total;
					BlueprinterLoadingScreen.UpdateText(orCreateBar.Text, orCreateBar.HasError, orCreateBar.BundleName, orCreateBar.BundleVer, current, total);
				}
			}
		}

		private void MarkDoneAndTryClose()
		{
			bool flag = !this.notDedicatedServer;
			if (!flag)
			{
				this.EnsureUniversalUI();
				bool flag2 = !this.barContainer;
				if (!flag2)
				{
					foreach (BlueprinterLoadingScreen.Bar bar in this.bars.Values)
					{
						bool flag3 = ((bar != null) ? bar.Text : null) == null;
						if (!flag3)
						{
							string text = bar.BundleName ?? "";
							bool flag4 = !text.StartsWith("(DONE)", StringComparison.Ordinal);
							if (flag4)
							{
								text = "(DONE) " + text;
							}
							int num = Mathf.Max(1, bar.Total);
							BlueprinterLoadingScreen.UpdateText(bar.Text, bar.HasError, text, bar.BundleVer ?? "", num, num);
							GameObject gameObject = bar.Text.gameObject;
							bool flag5 = gameObject && gameObject.name.StartsWith("2082LoadingBar_", StringComparison.Ordinal);
							if (flag5)
							{
								gameObject.name = "(DONE)" + gameObject.name;
							}
						}
					}
					foreach (object obj in this.barContainer)
					{
						Transform transform = (Transform)obj;
						bool flag6 = transform && transform.name.StartsWith("2082LoadingBar_", StringComparison.Ordinal);
						if (flag6)
						{
							return;
						}
					}
					GameObject gameObject2 = GameObject.Find("2082LoadOverlay");
					bool flag7 = gameObject2;
					if (flag7)
					{
						Object.Destroy(gameObject2);
					}
				}
			}
		}

		private void StretchToFill(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.one;
		}

		private void EnsureUniversalUI()
		{
			bool flag = !this.notDedicatedServer;
			if (!flag)
			{
				bool flag2 = this.barContainer;
				if (!flag2)
				{
					GameObject gameObject = GameObject.Find("2082LoadOverlay");
					bool flag3 = gameObject == null;
					if (flag3)
					{
						gameObject = new GameObject("2082LoadOverlay");
						Object.DontDestroyOnLoad(gameObject);
						Canvas canvas = gameObject.AddComponent<Canvas>();
						canvas.renderMode = RenderMode.ScreenSpaceOverlay;
						canvas.sortingOrder = 31000;
						CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
						canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
						canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
						gameObject.AddComponent<GraphicRaycaster>();
						GameObject gameObject2 = new GameObject("BlueprinterTint");
						gameObject2.transform.SetParent(gameObject.transform, false);
						Image image = gameObject2.AddComponent<Image>();
						image.color = new Color(0f, 0f, 0f, 0.9f);
						this.StretchToFill(gameObject2.GetComponent<RectTransform>());
						GameObject gameObject3 = new GameObject("BlueprinterHeader");
						gameObject3.transform.SetParent(gameObject.transform, false);
						Text text = gameObject3.AddComponent<Text>();
						text.font = this.GetBestFontCached();
						text.fontSize = 45;
						text.alignment = TextAnchor.UpperCenter;
						text.text = "Loading...";
						text.color = new Color(0.35f, 0.88f, 0.53f, 0.9f);
						RectTransform component = gameObject3.GetComponent<RectTransform>();
						this.StretchToFill(component);
						component.pivot = new Vector2(0.5f, 1f);
						component.anchoredPosition = new Vector2(0f, -50f);
						component.sizeDelta = new Vector2(0f, 100f);
					}
					Transform transform = gameObject.transform.Find("LoadingBarsContainer");
					bool flag4 = transform == null;
					if (flag4)
					{
						GameObject gameObject4 = new GameObject("LoadingBarsContainer");
						gameObject4.transform.SetParent(gameObject.transform, false);
						RectTransform rectTransform = gameObject4.AddComponent<RectTransform>();
						rectTransform.anchorMin = new Vector2(0f, 1f);
						rectTransform.anchorMax = new Vector2(0f, 1f);
						rectTransform.pivot = new Vector2(0f, 1f);
						rectTransform.anchoredPosition = new Vector2(100f, -100f);
						rectTransform.sizeDelta = new Vector2(800f, 0f);
						VerticalLayoutGroup verticalLayoutGroup = gameObject4.AddComponent<VerticalLayoutGroup>();
						verticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
						verticalLayoutGroup.spacing = 100f;
						verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
						verticalLayoutGroup.childControlHeight = true;
						verticalLayoutGroup.childControlWidth = true;
						verticalLayoutGroup.childForceExpandHeight = false;
						verticalLayoutGroup.childForceExpandWidth = true;
						transform = gameObject4.transform;
					}
					this.barContainer = transform;
				}
			}
		}

		private BlueprinterLoadingScreen.Bar GetOrCreateBar(string key)
		{
			BlueprinterLoadingScreen.Bar bar;
			bool flag = this.bars.TryGetValue(key, out bar) && ((bar != null) ? bar.Text : null);
			BlueprinterLoadingScreen.Bar bar2;
			if (flag)
			{
				bar2 = bar;
			}
			else
			{
				string text = string.Format("{0}{1}_{2}", "2082LoadingBar_", "com.nikkorap.blueprinter", Animator.StringToHash(key));
				Transform transform = this.barContainer.Find(text);
				GameObject gameObject = (transform ? transform.gameObject : null);
				bool flag2 = gameObject == null;
				if (flag2)
				{
					gameObject = new GameObject(text);
					gameObject.transform.SetParent(this.barContainer, false);
					LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
					layoutElement.preferredHeight = 200f;
					layoutElement.flexibleHeight = 0f;
				}
				Text text2 = gameObject.GetComponent<Text>() ?? gameObject.AddComponent<Text>();
				text2.font = this.GetBestFontCached();
				text2.color = Color.white;
				text2.alignment = TextAnchor.UpperLeft;
				text2.fontSize = 25;
				text2.horizontalOverflow = HorizontalWrapMode.Overflow;
				text2.verticalOverflow = VerticalWrapMode.Overflow;
				text2.lineSpacing = 1.33f;
				text2.supportRichText = true;
				BlueprinterLoadingScreen.Bar bar3 = new BlueprinterLoadingScreen.Bar
				{
					Text = text2
				};
				this.bars[key] = bar3;
				bar2 = bar3;
			}
			return bar2;
		}

		private Font GetBestFontCached()
		{
			bool flag = this.cachedFont;
			Font font;
			if (flag)
			{
				font = this.cachedFont;
			}
			else
			{
				foreach (Font font2 in Resources.FindObjectsOfTypeAll<Font>())
				{
					bool flag2 = font2.name == "BrassMono-Regular" || font2.name == "Barlow-Regular";
					if (flag2)
					{
						this.cachedFont = font2;
						return this.cachedFont;
					}
				}
				this.cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 13) ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
				font = this.cachedFont;
			}
			return font;
		}

		private static void UpdateText(Text text, bool hasError, string bundleName, string bundleVersion, int current, int total)
		{
			bool flag = !text;
			if (!flag)
			{
				int num = Mathf.Clamp(Mathf.RoundToInt((float)(20 * (current + 1)) / (float)(total + 1)), 0, 20);
				hasError = false;
				StringBuilder stringBuilder = new StringBuilder(256);
				stringBuilder.Append(string.Concat(new string[] { "<color=#f5f5f5ff>Processing ", bundleName, "-", bundleVersion, "</color>\n" }));
				stringBuilder.Append("<color=#f5f5f5ff>[</color>");
				stringBuilder.Append(string.Concat(new string[]
				{
					"<color=",
					hasError ? "#FF4444" : "#58e187ff",
					">",
					new string('=', num),
					"</color>"
				}));
				stringBuilder.Append("<color=#f5f5f540>" + new string('=', 20 - num) + "</color>");
				stringBuilder.Append("<color=#f5f5f5ff>] </color>");
				stringBuilder.Append(string.Format("<color={0}>{1}/{2}</color>\n", "#f5f5f5ff", current, total));
				text.text = stringBuilder.ToString();
			}
		}

		private const string OverlayName = "2082LoadOverlay";

		private const string ContainerName = "LoadingBarsContainer";

		private const string ActivePrefix = "2082LoadingBar_";

		private const string DonePrefix = "(DONE)";

		private readonly Dictionary<string, BlueprinterLoadingScreen.Bar> bars = new Dictionary<string, BlueprinterLoadingScreen.Bar>(StringComparer.Ordinal);

		private Transform barContainer;

		private Font cachedFont;

		private const int BarChars = 20;

		private const string primaryWhite = "#f5f5f5ff";

		private const string secondaryWhite = "#f5f5f540";

		private const string primaryGreen = "#58e187ff";

		private const string red = "#FF4444";

		private sealed class Bar
		{
			public Text Text;

			public bool HasError;

			public string BundleName;

			public string BundleVer;

			public int Current;

			public int Total;
		}
	}
}
