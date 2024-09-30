using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModAssetLoader {
	[BepInPlugin("gay.crf.modiomori.assetloader", "AssetLoader", "1.0.0")]
	public class AssetLoaderPlugin : BaseUnityPlugin {
		public static ManualLogSource? Log;
		private void Awake() {
			Log = Logger;
			Harmony.CreateAndPatchAll(typeof(_MapPatches));
			Chainloader.ManagerObject.hideFlags = HideFlags.HideAndDontSave;
			SceneManager.activeSceneChanged += MapLoader._SceneChangeHook;

			string ModPath = Path.GetDirectoryName(Info.Location);
			AssetBundle NewBundle =
			    AssetBundle.LoadFromFile(Path.Combine(ModPath, "maploadertest.bundle"));
			MapLoader.AddMap("cs_office", NewBundle.GetAllScenePaths()[0]);

			Logger.LogInfo($"Plugin AssetLoader is loaded!");
			Logger.LogInfo(NewBundle.GetAllScenePaths()[0]);
		}
	}
}
