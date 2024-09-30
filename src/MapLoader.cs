using HarmonyLib;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using static System.Net.Mime.MediaTypeNames;

namespace ModAssetLoader {
	public static class MapLoader {
		internal static Dictionary<string, string> CustomMaps = new Dictionary<string, string>();
		internal static List<MapItem> CustomMapItems = new List<MapItem>();

		// Name is the name of your map's scene excluding the 'map_' portion. map_ExampleMap would
		// be ExampleMap.
		public static void AddMap(string name, string path, string displayName,
		                          Texture background) {
			MapItem Item = new MapItem { mapName = displayName, backgroundImage = background };
			if (GameSettingsPanel.singleton != null)
				GameSettingsPanel.singleton.mapList.Add(Item);

			CustomMapItems.Add(Item);
			CustomMaps.Add(name, path);
		}

		public static void AddMap(string name, string path, string displayName) {
			AddMap(name, path, displayName, Texture2D.blackTexture);
		}

		public static void AddMap(string name, string path) {
			AddMap(name, path, name, Texture2D.blackTexture);
		}

		internal static bool IsValidMapName(string name) {
			if (name == null || name.Length < 5 || !name.StartsWith("map_"))
				return false;

			return true;
		}

		internal static void _SceneChangeHook(Scene current, Scene next) {
			if (next == null || !IsValidMapName(next.name))
				return;

			if (!MapLoader.CustomMaps.ContainsKey(next.name.Substring(4)))
				return;

			IEnumerable<Object> CandidateProfiles =
			    Resources.FindObjectsOfTypeAll(typeof(VolumeProfile))
			        .Where(Object => Object.name == "Effects Profile");
			if (CandidateProfiles.Count() > 0) {
				VolumeProfile MainProfile = (VolumeProfile)CandidateProfiles.First();
				AssetLoaderPlugin.Log!.LogInfo("Got effects profile");
			}

			Shader MainShader = Shader.Find("Universal Render Pipeline/Lit");
			Renderer[] AllRenderers = Object.FindObjectsOfType<Renderer>();
			foreach (Renderer Renderer in AllRenderers) {
				foreach (Material Material in Renderer.materials) {
					Texture OldTexture = Material.mainTexture;
					Material.shader = MainShader;
					Material.mainTexture = OldTexture;
				}
			}
		}
	}

	[HarmonyPatch]
	internal static class _MapPatches {
		[HarmonyPrefix, HarmonyPatch(typeof(GameSettingsPanel), "Start")]
		private static void EnableMapSelectHook(GameSettingsPanel __instance) {
			__instance.mapSelect.gameObject.SetActive(true);

			// These scenes don't exist in the demo.
			__instance.mapList.RemoveAll(mapItem => mapItem.mapName == "The Specialists Dojo" ||
			                                        mapItem.mapName == "Die By The Lava");
			__instance.mapList = __instance.mapList.Concat(MapLoader.CustomMapItems).ToList();
		}

		[HarmonyPrefix, HarmonyPatch(typeof(MusicManager), "SetSong")]
		private static void FixMapMusicHook(MusicManager __instance, string sceneName) {
			if (!MapLoader.IsValidMapName(sceneName))
				return;

			if (!MapLoader.CustomMaps.ContainsKey(sceneName.Substring(4)))
				return;

			SongSound SongSound =
			    __instance.musicList.Where((SongSound x) => x.sceneName == sceneName)
			        .FirstOrDefault<SongSound>();
			if (SongSound != null)
				return;

			AudioClip[] AudioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
			AudioClip? TargetMusic = AudioClips.Where((a) => a.name == "JOHN-ASS").FirstOrDefault();
			if (TargetMusic == null)
				return;

			SongSound NewSound = new SongSound { AudioClip = TargetMusic, sceneName = sceneName };
			__instance.musicList.Add(NewSound);
		}

		[HarmonyPostfix, HarmonyPatch(typeof(GameSettingsPanel), "MapChanged")]
		private static void FixMapPathHook(GameSettingsPanel __instance, bool ___isMultiplayer,
		                                   MultiplayerRoomManager ___roomManager) {
			string MapToLoad = __instance.mapSelect.GetCurrentValue.optionValue;
			if (!MapLoader.CustomMaps.ContainsKey(MapToLoad))
				return;

			if (___isMultiplayer && ___roomManager.mode == NetworkManagerMode.Host)
				___roomManager.SetGameplayScene(MapLoader.CustomMaps[MapToLoad]);
			if (IGameSettingsManager.singleton != null)
				IGameSettingsManager.singleton.SelectedMap = MapLoader.CustomMaps[MapToLoad];
		}
	}
}
