#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    [InitializeOnLoad]
    public static class AutoSetup
    {
        private const string SetupDoneKey = "GeminiLab.AutoSetupDone";
        private const int ExpectedVersion = 71;

        static AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                int currentVersion = EditorPrefs.GetInt(SetupDoneKey, 0);
                bool needsLatestAuthoring = RequiresLatestAuthoring();
                if (currentVersion >= ExpectedVersion && !needsLatestAuthoring) return;

                if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                {
                    Debug.LogWarning("[AutoSetup] 当前处于 PlayMode，暂不执行场景作者化；停止运行后请重新触发 AutoSetup。 ");
                    return;
                }

                try
                {
                    Debug.Log($"[AutoSetup] 版本 {currentVersion} → {ExpectedVersion}，开始升级...");

                    if (currentVersion < 1)
                    {
                        BootEmotionGardenBootstrapAuthoring.Author();

                        if (GameObject.Find("_SceneRoot") == null)
                            WorldMapSceneAuthoring.Author();

                        WorldMapSceneObjectsPatch.Patch();
                        WorldMapGardenZonePatch.Patch();
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 2)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                        WorldMapGardenZonePatch.Patch();
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 3)
                    {
                        FixPetHorizontalOnly();
                    }

                    if (currentVersion < 4)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 5)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 6)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 7)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 8)
                    {
                        WorldMapGardenZonePatch.Patch();
                    }

                    if (currentVersion < 9)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 10)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 11)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 12)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 13)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 14)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 15)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 16)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 17)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 18)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                    }

                    if (currentVersion < 19)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 20)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 21)
                    {
                        WorldMapSceneObjectsPatch.Patch();
                    }

                    if (currentVersion < 22)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 23)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 24)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 25)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 26)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 27)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 28)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 29)
                    {
                        WorldMapDayNightAuthoring.Patch();
                    }

                    if (currentVersion < 30)
                    {
                        WorldMapPetAnimationTriggerAuthoring.Patch();
                    }

                    if (currentVersion < 31)
                    {
                        WorldMapInteractiveObjectAuthoring.Patch();
                    }

                    if (currentVersion < 32)
                    {
                        WorldMapPetAnimationTriggerAuthoring.Patch();
                    }

                    if (currentVersion < 36)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 37)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 38)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 39)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 40)
                    {
                        // 修复每周培育瓶子/星期标签与图鉴锁定态的 Scene 作者化。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 41)
                    {
                        // 瓶子使用固定作者化资源，不再绑定不存在的状态变体。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 42)
                    {
                        // 修复每周空状态、UIbar真实数据、图鉴变体和详情页逐花土壤布局。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 43)
                    {
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 44)
                    {
                        // 每周培育面板改为单一集中 UIbar，并作者化瓶子选择交互。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 45)
                    {
                        // 修正瓶子选中外圈节点的作者化默认状态。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 46)
                    {
                        // 将瓶子选中效果改为只输出 Sprite Alpha 边缘，避免整瓶染色。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 47)
                    {
                        // 花朵自由摆放改为使用 arrange 美术资源的左侧滚动侧边栏。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 48)
                    {
                        // 修复 BaselineItem 分层网格、相邻层半格错位和花丛单格尺寸。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 49)
                    {
                        // 重新落盘最新的层级基线、错位网格和花丛 3 单格尺寸。
                        WorldMapEmotionGardenUIPatch.Patch();
                    }

                    if (currentVersion < 50)
                    {
                        // 仅将草地区域内的 BaselineItem 作为可吸附层，并保持网格视觉关闭。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                    }

                    if (currentVersion < 51)
                    {
                        // 再次落盘摆放层过滤结果，确保已经打开的场景也刷新作者化数据。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                    }

                    if (currentVersion < 52)
                    {
                        // Patch 会修改当前场景中的序列化层列表；这里显式保存，避免只改内存而下次启动丢失。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 53)
                    {
                        // 清理槽位上的旧版重复组件，并重新落盘花丛 3 的实际占用尺寸。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 54)
                    {
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 55)
                    {
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 56)
                    {
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 57)
                    {
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 58)
                    {
                        // 清理旧版丢失脚本的 PlacementSlot，并重新挂载当前 WorldMapPlacementSlot。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 59)
                    {
                        // 拆分 PlacementSlot 独立脚本后重新落盘 32 个槽位，清除旧嵌入式 MonoScript 引用。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 60)
                    {
                        // 统一花卉与桌宠的 Default Sorting Layer，并清理旧摆放视觉的排序配置。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 61)
                    {
                        // 撤销花朵全局前置排序，改为花朵与桌宠共享 BaselineItem 层级。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 62)
                    {
                        // 为 WorldMap 桌宠根对象补齐 BaselineItem，并保存基线排序参数。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 63)
                    {
                        // 保持桌宠原有胶囊碰撞体为实体碰撞，不让 BaselineItem 改成 Trigger。
                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            WorldMapFlowerPlacementAuthoring.Patch(canvas, SortingLayer.NameToID("UI"));
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        }
                    }

                    if (currentVersion < 64)
                    {
                        BootAppleBootstrapAuthoring.Author();
                        WorldMapAppleTreeAuthoring.Patch();
                        ApartmentAppleBalanceAuthoring.Patch();
                    }

                    if (currentVersion < 65)
                    {
                        ApartmentAppleBalanceAuthoring.Patch();
                    }

                    if (currentVersion < 66 || needsLatestAuthoring)
                    {
                        DailySummaryMailboxAuthoring.Patch();
                        WorldMapOutdoorPetAnimationAuthoring.Patch();
                    }

                    if (currentVersion < 69)
                    {
                        ApartmentFurnitureSelectionAuthoring.Patch();
                    }

                    if (currentVersion < 70)
                    {
                        WorldMapAppleTreeAuthoring.Patch();
                    }

                    if (currentVersion < 71 || needsLatestAuthoring)
                    {
                        // 只应用本次 UI 增量，避免重建已有场景层级和花朵对象。
                        WorldMapEmotionGardenUIPatch.PatchUiTaskMinimizedAll();
                    }

                    EditorPrefs.SetInt(SetupDoneKey, ExpectedVersion);
                    Debug.Log($"[AutoSetup] 升级到版本 {ExpectedVersion} 完成。");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoSetup] 失败: {e.Message}\n{e.StackTrace}");
                }
            };
        }

        /// <summary>修复 WorldMap 场景中桌宠的横板移动配置。</summary>
        private static void FixPetHorizontalOnly()
        {
            const string scenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
            if (!System.IO.File.Exists(scenePath)) return;

            var scene = EditorSceneManager.GetActiveScene().path == scenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool dirty = false;

            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var input in go.GetComponentsInChildren<PetPlayerInputController>(true))
                {
                    // 移除重复的 RandomWander
                    var wanders = input.GetComponents<RandomWander>();
                    if (wanders.Length > 1)
                    {
                        for (int i = wanders.Length - 1; i >= 0; i--)
                        {
                            if (wanders[i] != null)
                                Object.DestroyImmediate(wanders[i]);
                        }
                        // 重新加一个干净的
                        var w = input.gameObject.AddComponent<RandomWander>();
                        SetupHorizontalWander(w);
                        dirty = true;
                    }
                    else if (wanders.Length == 1)
                    {
                        SetupHorizontalWander(wanders[0]);
                        dirty = true;
                    }

                    // PetPlayerInputController 横板模式
                    var inputSo = new SerializedObject(input);
                    var hProp = inputSo.FindProperty("_horizontalOnly");
                    if (hProp != null && !hProp.boolValue)
                    {
                        hProp.boolValue = true;
                        inputSo.ApplyModifiedProperties();
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[AutoSetup] 桌宠横板移动配置已修复（RandomWander + PetPlayerInputController._horizontalOnly = true）");
            }
        }

        private static void SetupHorizontalWander(RandomWander w)
        {
            var boundsGo = GameObject.Find("PetMovementBounds");
            if (boundsGo == null) return;

            var col = boundsGo.GetComponent<BoxCollider2D>();
            if (col == null) return;

            Vector2 center = (Vector2)boundsGo.transform.position + col.offset;
            Vector2 halfSize = col.size * 0.5f;
            var so = new SerializedObject(w);
            so.FindProperty("_boundsMin").vector2Value = center - halfSize;
            so.FindProperty("_boundsMax").vector2Value = center + halfSize;
            so.FindProperty("_moveSpeed").floatValue = 1.2f;
            so.FindProperty("_horizontalOnly").boolValue = true;
            so.ApplyModifiedProperties();
        }

        private static bool RequiresLatestAuthoring()
        {
            const string apartmentScene = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
            const string worldMapScene = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
            if (!System.IO.File.Exists(apartmentScene) || !System.IO.File.Exists(worldMapScene))
            {
                return true;
            }

            string apartmentText = System.IO.File.ReadAllText(apartmentScene);
            string worldMapText = System.IO.File.ReadAllText(worldMapScene);
            int freeControlCount = worldMapText.Split(
                new[] { "_preferControlOnEnable: 0" },
                System.StringSplitOptions.None).Length - 1;
            int gardenZoneScriptCount = CountOccurrences(worldMapText, "guid: e808b994ed5a1844f8fd069d9c18ff9d");
            return !apartmentText.Contains("MailboxButton", System.StringComparison.Ordinal) ||
                   !worldMapText.Contains("c24671ba6d25cb246bdc627378a8fa9e", System.StringComparison.Ordinal) ||
                   freeControlCount < 2 ||
                   !worldMapText.Contains("WorldMapAppleDrops", System.StringComparison.Ordinal) ||
                   !worldMapText.Contains("AppleTreeDropController", System.StringComparison.Ordinal) ||
                   !worldMapText.Contains("InputVisual_Focused", System.StringComparison.Ordinal) ||
                   !worldMapText.Contains("WishInputFocusedVisual", System.StringComparison.Ordinal) ||
                   gardenZoneScriptCount < 4;
        }

        private static int CountOccurrences(string text, string value)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) return 0;
            int count = 0;
            int offset = 0;
            while ((offset = text.IndexOf(value, offset, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += value.Length;
            }

            return count;
        }

        public static void Reset()
        {
            EditorPrefs.DeleteKey(SetupDoneKey);
            Debug.Log("[AutoSetup] 已重置，下次编译后重新执行初始化。");
        }
    }
}
#endif
