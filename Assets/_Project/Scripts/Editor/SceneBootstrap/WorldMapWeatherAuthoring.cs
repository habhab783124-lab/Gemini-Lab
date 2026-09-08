#nullable enable
#if UNITY_EDITOR
using System;
using System.Linq;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Authors the saved WorldMap weather overlays.  The cloud image is a
    /// derived alpha sprite generated from the supplied JPG; the source art is
    /// never modified and no color-key material is assigned at runtime.
    /// </summary>
    public static class WorldMapWeatherAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string WeatherArtFolder = "Assets/_Project/Art/WorldMap/weather";
        private const string RainSpritePath = WeatherArtFolder + "/rain.png";
        private const string CloudSpritePath = "Assets/_Project/Art/WorldMap/苹果云背景补充/cloud.png";
        private const string StarsSpritePath = WeatherArtFolder + "/\u661f\u661f.PNG";
        private const string SunnyName = "WorldMapWeatherSunnyOverlay";
        private const string RainName = "WorldMapWeatherRainOverlay";
        private const string CloudName = "WorldMapWeatherClouds";
        private const string StarsName = "WorldMapWeatherStars";
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
            Sprite? cloudSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloudSpritePath);
            Sprite? starsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarsSpritePath);
            if (rainSprite is null || cloudSprite is null || starsSprite is null)
            {
                Debug.LogError($"[WorldMapWeather] Missing weather Sprite. rain={rainSprite != null}, cloud={cloudSprite != null}, stars={starsSprite != null}");
                return;
            }

            SpriteRenderer? sunny = FindOverlay(SunnyName);
            if (sunny is not null)
            {
                Undo.DestroyObjectImmediate(sunny.gameObject);
            }

            SpriteRenderer rain = EnsureOverlay(RainName, rainSprite);
            ConfigureRenderer(rain, rainSprite);

            SpriteRenderer clouds = EnsureOverlay(CloudName, cloudSprite);
            SpriteRenderer stars = EnsureOverlay(StarsName, starsSprite);
            Material? spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            ConfigureEnvironmentRenderer(clouds, cloudSprite, "Environment_Clouds", spriteMaterial);
            ConfigureEnvironmentRenderer(stars, starsSprite, "Environment_Stars", null);
            stars.enabled = IsNight(DateTime.Now);

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
            EditorUtility.SetDirty(clouds);
            EditorUtility.SetDirty(stars);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapWeather] Rain, cloud, and star overlays authored from the weather folder.");
        }

        private static SpriteRenderer? FindOverlay(string objectName)
        {
            GameObject? existing = UnityEngine.Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == objectName);
            return existing?.GetComponent<SpriteRenderer>();
        }

        private static SpriteRenderer EnsureOverlay(string objectName, Sprite sprite)
        {
            GameObject? existing = UnityEngine.Object.FindObjectsByType<GameObject>(
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

        private static void ConfigureEnvironmentRenderer(
            SpriteRenderer renderer,
            Sprite sprite,
            string baselineId,
            Material? spriteMaterial)
        {
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            if (baselineId == "Environment_Clouds")
            {
                // Alpha is authored into the derived PNG.  Use Unity's
                // standard sprite material; a null material is not rendered
                // correctly by this project's configured 2D pipeline.
                renderer.sharedMaterial = spriteMaterial;
            }

            SpriteRenderer? background = FindBackgroundRenderer();
            if (background != null && sprite.bounds.size.x > 0f)
            {
                renderer.transform.position = background.transform.position;
                renderer.transform.rotation = background.transform.rotation;
                if (baselineId == "Environment_Clouds")
                {
                    float viewportWidth = FindWorldMapViewportWidth();
                    float widthScale = viewportWidth / sprite.bounds.size.x;
                    renderer.transform.localScale = new Vector3(widthScale, widthScale, 1f);
                }
                else
                {
                    float widthScale = background.bounds.size.x / sprite.bounds.size.x;
                    float heightScale = background.bounds.size.y / sprite.bounds.size.y;
                    renderer.transform.localScale = new Vector3(widthScale, heightScale, 1f);
                }
            }

            WorldMapBaselineDefinition? definition = FindBaselineDefinition(baselineId);
            if (definition == null)
            {
                Debug.LogWarning($"[WorldMapWeather] Baseline definition not found: {baselineId}");
                return;
            }

            if (baselineId == "Environment_Clouds")
            {
                // The cloud strip contains transparent margins below the
                // artwork. Align its authored object to the cloud baseline so
                // the visible silhouettes sit in the sky instead of behind
                // the apartment roof.
                Vector3 position = renderer.transform.position;
                position.y = definition.BaselineY;
                renderer.transform.position = position;
            }

            BoxCollider2D collider = renderer.GetComponent<BoxCollider2D>()
                ?? renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.enabled = false;
            collider.isTrigger = true;

            BaselineItem item = renderer.GetComponent<BaselineItem>()
                ?? renderer.gameObject.AddComponent<BaselineItem>();
            SerializedObject serialized = new(item);
            serialized.FindProperty("_baselineDefinition")!.objectReferenceValue = definition;
            serialized.FindProperty("_allowDrag")!.boolValue = false;
            serialized.FindProperty("_solidCollider")!.boolValue = false;
            serialized.FindProperty("_baselineTransformOffset")!.floatValue =
                renderer.transform.position.y - definition.BaselineY;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            item.RefreshBaselineBinding();
            EditorUtility.SetDirty(item);
        }

        private static SpriteRenderer? FindBackgroundRenderer()
        {
            Sprite? backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                WeatherArtFolder + "/\u5929\u7A7A.jpg");
            if (backgroundSprite == null) return null;

            return UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.sprite == backgroundSprite);
        }

        private static float FindWorldMapViewportWidth()
        {
            Camera? camera = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.enabled && candidate.orthographic && candidate.gameObject.scene.IsValid());
            if (camera == null || camera.aspect <= 0f)
            {
                return 13.333333f;
            }

            return camera.orthographicSize * 2f * camera.aspect;
        }

        private static WorldMapBaselineDefinition? FindBaselineDefinition(string baselineId)
        {
            return UnityEngine.Object.FindObjectsByType<WorldMapBaselineDefinition>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.Id == baselineId);
        }

        private static bool IsNight(DateTime localTime)
        {
            TimeSpan time = localTime.TimeOfDay;
            return time < TimeSpan.FromHours(6f) || time >= TimeSpan.FromHours(18f);
        }
    }
}
#endif
