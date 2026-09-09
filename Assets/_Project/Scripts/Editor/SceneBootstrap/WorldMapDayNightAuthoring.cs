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
    /// 将现有夜幕 Sprite 作者化到 WorldMap 场景，并保存当前真实时间的初始显示状态。
    /// </summary>
    public static class WorldMapDayNightAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const string NightSpritePath = "Assets/_Project/Art/WorldMap/weather/夜幕.png";
        private const string OverlayName = "WorldMapNightOverlay";
        private const string NightSortingLayerName = "WorldMapNightOverlay";
        private const float DayStartHour = 6f;
        private const float NightStartHour = 18f;
        private const int OverlaySortingOrder = 2000;

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Day Night")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapDayNight] 当前处于 PlayMode，跳过昼夜场景作者化；请停止运行后重新执行。 ");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Sprite? nightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NightSpritePath);
            if (nightSprite == null)
            {
                Debug.LogError($"[WorldMapDayNight] 未找到夜幕 Sprite：{NightSpritePath}");
                return;
            }

            SpriteRenderer? renderer = FindNightRenderer(nightSprite);
            if (renderer == null)
            {
                var overlay = new GameObject(OverlayName);
                overlay.transform.SetParent(null);
                renderer = overlay.AddComponent<SpriteRenderer>();
                renderer.sprite = nightSprite;
            }

            GameObject overlayGo = renderer.gameObject;
            overlayGo.name = OverlayName;
            PositionOverlay(overlayGo.transform);

            renderer.sprite = nightSprite;
            renderer.sortingLayerName = NightSortingLayerName;
            renderer.sortingOrder = OverlaySortingOrder;

            foreach (Collider2D collider in overlayGo.GetComponents<Collider2D>())
            {
                collider.enabled = false;
                EditorUtility.SetDirty(collider);
            }

            var controller = overlayGo.GetComponent<WorldMapDayNightController>();
            if (controller == null)
            {
                controller = overlayGo.AddComponent<WorldMapDayNightController>();
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_nightOverlay")!.objectReferenceValue = renderer;
            SpriteRenderer? stars = FindStarsRenderer();
            if (stars != null)
            {
                serialized.FindProperty("_starsRenderer")!.objectReferenceValue = stars;
                stars.enabled = IsNight(DateTime.Now);
                EditorUtility.SetDirty(stars);
            }
            serialized.FindProperty("_dayStartHour")!.intValue = (int)DayStartHour;
            serialized.FindProperty("_nightStartHour")!.intValue = (int)NightStartHour;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            renderer.enabled = IsNight(DateTime.Now);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WorldMapDayNight] 夜幕已作者化，当前状态：{(renderer.enabled ? "夜晚" : "白天")}");
        }

        private static SpriteRenderer? FindNightRenderer(Sprite nightSprite)
        {
            return UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.sprite == nightSprite);
        }

        private static void PositionOverlay(Transform overlayTransform)
        {
            const float cameraCenterX = 1.5f;
            const float cameraCenterY = 2.4f;
            overlayTransform.SetParent(null);
            overlayTransform.position = new Vector3(cameraCenterX, cameraCenterY, 0f);

            // The existing artwork is wide enough for the full horizontal camera travel.
            // Keep its authored scale when it already exists; use a full-scene default for
            // a newly created renderer.
            if (overlayTransform.localScale == Vector3.one)
            {
                overlayTransform.localScale = new Vector3(1.8333956f, 1.0300167f, 1f);
            }
        }

        private static bool IsNight(DateTime localTime)
        {
            TimeSpan time = localTime.TimeOfDay;
            return time < TimeSpan.FromHours(DayStartHour) || time >= TimeSpan.FromHours(NightStartHour);
        }

        private static SpriteRenderer? FindStarsRenderer()
        {
            return UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.gameObject.name == "WorldMapWeatherStars");
        }
    }
}
#endif
