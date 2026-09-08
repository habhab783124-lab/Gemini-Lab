#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 将 WorldMap 中已经存在的场景物作者化为可悬停、可点击对象。
    /// 只补组件和序列化参数，不创建新的视觉占位物，也不覆盖对象的现有位置、Sprite 或 Collider 尺寸。
    /// </summary>
    public static class WorldMapInteractiveObjectAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const float DefaultHoverScaleMultiplier = 1.06f;
        private const float DefaultTransitionSeconds = 0.08f;

        private static readonly (string Name, bool IsCabin)[] Targets =
        {
            ("室内", true),
            ("邮箱", false),
            ("大树 1", false),
            ("大树 2", false),
            ("大树 3", false),
            ("大树 4", false),
            ("大树 5", false)
        };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;
            foreach (var target in Targets)
            {
                var go = GameObject.Find(target.Name);
                if (go == null)
                {
                    Debug.LogWarning($"[WorldMapInteractiveObjectAuthoring] 未找到「{target.Name}」，跳过");
                    continue;
                }

                if (IsSilhouetteTree(target.Name))
                {
                    changed |= EnsureSilhouetteCollider(go, target.Name);
                }
                else
                {
                    changed |= EnsureCollider(go, target.Name);
                }
                changed |= EnsureFeedback(go, target.Name);

                if (target.IsCabin)
                {
                    changed |= EnsureCabinPortal(go);
                }
                else if (target.Name == "大树 1")
                {
                    changed |= RemoveClickable(go);
                }
                else
                {
                    changed |= EnsureClickable(go, target.Name);
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[WorldMapInteractiveObjectAuthoring] 室内、邮箱和大树 2～5 的悬停缩放与点击入口已作者化；大树 1 仅保留悬停反馈");
        }

        private static bool EnsureCollider(GameObject go, string displayName)
        {
            var collider = go.GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider2D>(go);
                collider.isTrigger = false;
                Debug.Log($"[WorldMapInteractiveObjectAuthoring] {displayName} 已补 BoxCollider2D");
                return true;
            }

            if (collider.isTrigger)
            {
                collider.isTrigger = false;
                EditorUtility.SetDirty(collider);
                return true;
            }

            return false;
        }

        private static bool IsSilhouetteTree(string displayName)
        {
            return displayName == "大树 2" ||
                   displayName == "大树 3" ||
                   displayName == "大树 4" ||
                   displayName == "大树 5";
        }

        private static bool EnsureSilhouetteCollider(GameObject go, string displayName)
        {
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                Debug.LogError($"[WorldMapInteractiveObjectAuthoring] {displayName} 缺少 SpriteRenderer 或 Sprite，无法生成轮廓碰撞体");
                return false;
            }

            Vector2[][]? generatedPaths = GenerateSpriteOutline(spriteRenderer.sprite);
            var validPaths = new List<Vector2[]>();
            if (generatedPaths != null)
            {
                foreach (var generatedPath in generatedPaths)
                {
                    if (generatedPath == null || generatedPath.Length < 3)
                    {
                        continue;
                    }

                    var path = new Vector2[generatedPath.Length];
                    for (int i = 0; i < generatedPath.Length; i++)
                    {
                        Vector2 point = generatedPath[i];
                        if (spriteRenderer.flipX)
                        {
                            point.x = -point.x;
                        }

                        if (spriteRenderer.flipY)
                        {
                            point.y = -point.y;
                        }

                        path[i] = point;
                    }

                    validPaths.Add(path);
                }
            }

            if (validPaths.Count == 0)
            {
                Debug.LogError($"[WorldMapInteractiveObjectAuthoring] {displayName} 的 Sprite 未生成有效透明轮廓，保留现有碰撞体");
                return false;
            }

            var polygon = go.GetComponent<PolygonCollider2D>();
            bool changed = false;
            if (polygon == null)
            {
                polygon = Undo.AddComponent<PolygonCollider2D>(go);
                changed = true;
            }

            if (polygon.isTrigger)
            {
                polygon.isTrigger = false;
                changed = true;
            }

            polygon.pathCount = validPaths.Count;
            for (int i = 0; i < validPaths.Count; i++)
            {
                polygon.SetPath(i, validPaths[i]);
            }

            EditorUtility.SetDirty(polygon);
            changed = true;

            var box = go.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Undo.DestroyObjectImmediate(box);
                changed = true;
            }

            Debug.Log($"[WorldMapInteractiveObjectAuthoring] {displayName} 已作者化 Sprite 轮廓 PolygonCollider2D（{validPaths.Count} 个路径）");
            return changed;
        }

        private static Vector2[][]? GenerateSpriteOutline(Sprite sprite)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (var utilityType in types)
                {
                    if (utilityType == null || utilityType.FullName == null ||
                        utilityType.FullName.IndexOf("SpriteUtility", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var methods = utilityType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var method in methods)
                    {
                        if (method.Name.IndexOf("GenerateOutline", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        var parameters = method.GetParameters();
                        if (parameters.Length != 4 ||
                            parameters[0].ParameterType != typeof(Sprite) ||
                            parameters[1].ParameterType != typeof(float) ||
                            parameters[3].ParameterType != typeof(bool))
                        {
                            continue;
                        }

                        object alphaTolerance = parameters[2].ParameterType == typeof(byte)
                            ? (object)(byte)8
                            : 8;
                        object? result = method.Invoke(null, new object[] { sprite, 0.02f, alphaTolerance, true });
                        return result as Vector2[][];
                    }
                }
            }

            Debug.LogError("[WorldMapInteractiveObjectAuthoring] 未找到可用的 Sprite 轮廓 API");
            return null;
        }

        private static bool EnsureFeedback(GameObject go, string displayName)
        {
            var feedback = go.GetComponent<WorldMapInteractiveObjectFeedback>();
            bool created = false;
            if (feedback == null)
            {
                feedback = Undo.AddComponent<WorldMapInteractiveObjectFeedback>(go);
                created = true;
            }

            var so = new SerializedObject(feedback);
            bool changed = false;
            if (created)
            {
                var multiplier = so.FindProperty("_hoverScaleMultiplier");
                if (multiplier != null)
                {
                    multiplier.floatValue = DefaultHoverScaleMultiplier;
                    changed = true;
                }

                var transition = so.FindProperty("_transitionSeconds");
                if (transition != null)
                {
                    transition.floatValue = DefaultTransitionSeconds;
                    changed = true;
                }

                var requireTopmost = so.FindProperty("_requireTopmostCollider");
                if (requireTopmost != null)
                {
                    requireTopmost.boolValue = false;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feedback);
            }

            return created || changed;
        }

        private static bool EnsureCabinPortal(GameObject go)
        {
            var oldClickable = go.GetComponent<ClickableSceneObject>();
            if (oldClickable != null)
            {
                UnityEngine.Object.DestroyImmediate(oldClickable);
            }

            if (go.GetComponent<CabinReturnPortal>() != null)
            {
                return oldClickable != null;
            }

            Undo.AddComponent<CabinReturnPortal>(go);
            return true;
        }

        private static bool EnsureClickable(GameObject go, string displayName)
        {
            var clickable = go.GetComponent<ClickableSceneObject>();
            bool created = false;
            if (clickable == null)
            {
                clickable = Undo.AddComponent<ClickableSceneObject>(go);
                created = true;
            }

            var so = new SerializedObject(clickable);
            var displayNameProperty = so.FindProperty("_displayName");
            var clickMessageProperty = so.FindProperty("_clickMessage");
            bool changed = false;

            if (displayNameProperty != null && displayNameProperty.stringValue != displayName)
            {
                displayNameProperty.stringValue = displayName;
                changed = true;
            }

            const string clickMessage = "点击了 {0}（具体交互待接入）";
            if (clickMessageProperty != null && clickMessageProperty.stringValue != clickMessage)
            {
                clickMessageProperty.stringValue = clickMessage;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(clickable);
            }

            return created || changed;
        }

        private static bool RemoveClickable(GameObject go)
        {
            var clickable = go.GetComponent<ClickableSceneObject>();
            if (clickable == null)
            {
                return false;
            }

            Undo.DestroyObjectImmediate(clickable);
            return true;
        }
    }
}
#endif
