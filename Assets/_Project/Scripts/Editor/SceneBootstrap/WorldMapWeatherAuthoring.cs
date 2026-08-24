#nullable enable
#if UNITY_EDITOR
using System.Linq;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the saved WorldMap weather overlay. Rain art is sourced from the
    /// dedicated weather folder; clear weather uses the authored scene background
    /// until a dedicated sunny asset is provided there.
    /// </summary>
    public static class WorldMapWeatherAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string WeatherArtFolder = "Assets/_Project/Art/WorldMap/weather";
        private const string RainSpritePath = WeatherArtFolder + "/rain.png";
        private const string SunnyName = "WorldMapWeatherSunnyOverlay";
        private const string RainName = "WorldMapWeatherRainOverlay";
        private const int SortingOrder = 1990;

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Weather")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapWeather] Skipping weather authoring while in PlayMode.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Sprite? rainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RainSpritePath);
            if (rainSprite is null)
            {
                Debug.LogError($"[WorldMapWeather] Missing rain Sprite: {RainSpritePath}");
                return;
            }

            SpriteRenderer? sunny = FindOverlay(SunnyName);
            if (sunny is not null)
            {
                Undo.DestroyObjectImmediate(sunny.gameObject);
            }

            SpriteRenderer rain = EnsureOverlay(RainName, rainSprite);
            ConfigureRenderer(rain, rainSprite);

            WorldMapWeatherController controller = rain.GetComponent<WorldMapWeatherController>()
                ?? Undo.AddComponent<WorldMapWeatherController>(rain.gameObject);
            SerializedObject serialized = new(controller);
            serialized.FindProperty("_sunnyOverlay")!.objectReferenceValue = null;
            serialized.FindProperty("_rainOverlay")!.objectReferenceValue = rain;
            serialized.FindProperty("_latitude")!.doubleValue = 31.2304d;
            serialized.FindProperty("_longitude")!.doubleValue = 121.4737d;
            serialized.FindProperty("_timezone")!.stringValue = "auto";
            serialized.FindProperty("_useRemoteWeather")!.boolValue = true;
            serialized.FindProperty("_refreshIntervalMinutes")!.floatValue = 30f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            rain.enabled = false;
            EditorUtility.SetDirty(rain);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapWeather] Rain overlay and scene-background sunny state authored.");
        }

        private static SpriteRenderer? FindOverlay(string objectName)
        {
            GameObject? existing = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName);
            return existing?.GetComponent<SpriteRenderer>();
        }

        private static SpriteRenderer EnsureOverlay(string objectName, Sprite sprite)
        {
            GameObject? existing = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName);
            GameObject overlay = existing ?? new GameObject(objectName);
            SpriteRenderer? renderer = overlay.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = overlay.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            return renderer;
        }

        private static void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite)
        {
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = SortingOrder;
            renderer.transform.position = new Vector3(1.5f, 2.4f, 0f);
            renderer.transform.localScale = new Vector3(1.8333956f, 1.0300167f, 1f);
        }
    }
}
#endif
