#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 将 WorldMap/pets 室外序列帧作者化为仅供 WorldMap 使用的动画资产和场景实例绑定。
    /// 不修改 Apartment 共用 Pet Prefab，也不定义状态之间的触发条件。
    /// </summary>
    public static class WorldMapOutdoorPetAnimationAuthoring
    {
        private const string AnimationRoot = "Assets/_Project/Animations/WorldMap/Pet";
        private const string AngelRoot = "Assets/_Project/Art/WorldMap/pets/天使室外";
        private const string DevilRoot = "Assets/_Project/Art/WorldMap/pets/恶魔室外";
        private const float FrameRate = 6f;

        [MenuItem("Tools/Gemini-Lab/WorldMap/Setup Outdoor Pet Animations")]
        public static void Patch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[WorldMapOutdoorPets] 当前处于 PlayMode，跳过场景作者化；请停止运行后重新执行。 ");
                return;
            }

            const string scenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            EnsureFolder(AnimationRoot);

            var angel = new PetAnimationSet(
                "Angel",
                LoadSprites(AngelRoot + "/待机"),
                LoadSprites(AngelRoot + "/走路"),
                new[]
                {
                    new NamedFrames("Outdoor_Sit", AngelRoot + "/坐地", true),
                    new NamedFrames("Outdoor_Happy", AngelRoot + "/开心", true),
                    new NamedFrames("Outdoor_Water", AngelRoot + "/浇水", false),
                    new NamedFrames("Outdoor_Pray", AngelRoot + "/祈祷", true)
                });

            var devil = new PetAnimationSet(
                "Devil",
                LoadSprites(DevilRoot + "/待机"),
                LoadSprites(DevilRoot + "/走路"),
                new[]
                {
                    new NamedFrames("Outdoor_Sleep", DevilRoot + "/睡觉", true),
                    new NamedFrames("Outdoor_Cast", DevilRoot + "/施法", false),
                    new NamedFrames("Outdoor_Proud", DevilRoot + "/得意", true)
                });

            AnimatorController angelController = CreateController(angel);
            AnimatorController devilController = CreateController(devil);

            BindScenePet(
                "Pet_Angel",
                angelController,
                angel.IdleFrames.FirstOrDefault(),
                sideFramesFaceLeft: true,
                boundsMin: new Vector2(-18f, -1.95f),
                boundsMax: new Vector2(18f, -1.65f));
            BindScenePet(
                "Pet_Devil",
                devilController,
                devil.IdleFrames.FirstOrDefault(),
                boundsMin: new Vector2(-18f, -1.95f),
                boundsMax: new Vector2(18f, -1.65f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity")
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            Debug.Log("[WorldMapOutdoorPets] 室外双宠 AnimationClip、无条件状态机和场景实例绑定已完成。");
        }

        private static AnimatorController CreateController(PetAnimationSet set)
        {
            if (set.IdleFrames.Count == 0 || set.MoveFrames.Count == 0)
            {
                throw new InvalidOperationException($"{set.PetName} 缺少待机或走路帧。");
            }

            AnimationClip idle = CreateOrUpdateClip($"{AnimationRoot}/WorldMap_{set.PetName}_Idle.anim", set.IdleFrames, true);
            AnimationClip move = CreateOrUpdateClip($"{AnimationRoot}/WorldMap_{set.PetName}_Move.anim", set.MoveFrames, true);
            var controllerPath = $"{AnimationRoot}/WorldMap_{set.PetName}.controller";
            AnimatorController? controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            SetStateMotion(stateMachine, "Idle_Front", idle);
            SetStateMotion(stateMachine, "Idle_Back", idle);
            SetStateMotion(stateMachine, "Idle_Side", idle);
            SetStateMotion(stateMachine, "Move_Front", move);
            SetStateMotion(stateMachine, "Move_Back", move);
            SetStateMotion(stateMachine, "Move_Side", move);

            foreach (NamedFrames action in set.Actions)
            {
                List<Sprite> frames = LoadSprites(action.Folder);
                if (frames.Count == 0) continue;
                AnimationClip clip = CreateOrUpdateClip(
                    $"{AnimationRoot}/WorldMap_{set.PetName}_{action.StateName}.anim",
                    frames,
                    action.Loop);
                SetStateMotion(stateMachine, action.StateName, clip);
            }

            AnimatorState? defaultState = FindState(stateMachine, "Idle_Front");
            if (defaultState != null) stateMachine.defaultState = defaultState;
            ClearTransitions(stateMachine);
            ConfigureMovementParameters(controller, stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureMovementParameters(
            AnimatorController controller,
            AnimatorStateMachine stateMachine)
        {
            // PetController 负责写入这些参数；WorldMap 的专用状态机必须显式声明并消费它们。
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveDir", AnimatorControllerParameterType.Int);

            AddMoveTransition(stateMachine, "Move_Front", 0);
            AddMoveTransition(stateMachine, "Move_Back", 1);
            AddMoveTransition(stateMachine, "Move_Side", 2);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (controller.parameters.Any(parameter => parameter.name == parameterName))
            {
                return;
            }

            controller.AddParameter(parameterName, parameterType);
        }

        private static void AddMoveTransition(
            AnimatorStateMachine stateMachine,
            string destinationStateName,
            int moveDirection)
        {
            AnimatorState? destination = FindState(stateMachine, destinationStateName);
            if (destination == null)
            {
                throw new InvalidOperationException(
                    $"WorldMap Pet Animator 缺少移动状态：{destinationStateName}");
            }

            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.offset = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            transition.AddCondition(AnimatorConditionMode.Equals, moveDirection, "MoveDir");
        }

        private static void BindScenePet(
            string petName,
            AnimatorController controller,
            Sprite? idleSprite,
            bool? sideFramesFaceLeft = null,
            Vector2? boundsMin = null,
            Vector2? boundsMax = null)
        {
            GameObject? pet = GameObject.Find(petName);
            if (pet == null)
            {
                Debug.LogWarning($"[WorldMapOutdoorPets] 未找到 WorldMap 场景实例：{petName}");
                return;
            }

            // WorldMap owns its visual presentation.  Break the Apartment prefab link
            // before assigning the outdoor renderer and controller so Scene and Play
            // use the same serialized visual data.
            GameObject? prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(pet);
            if (prefabRoot != null)
            {
                PrefabUtility.UnpackPrefabInstance(
                    prefabRoot,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);

                // Unpacking can rebuild the scene hierarchy. Rebind the name after
                // the operation so subsequent component access targets the saved object.
                GameObject? reboundPet = GameObject.Find(petName);
                if (reboundPet != null)
                {
                    pet = reboundPet;
                }
            }

            Animator? animator;
            if (!pet.TryGetComponent<Animator>(out animator))
            {
                animator = pet.AddComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[WorldMapOutdoorPets] 场景对象 {petName} 无法获取或添加 Animator，已跳过室外动画绑定。");
                return;
            }

            animator.runtimeAnimatorController = controller;
            var renderer = pet.GetComponent<SpriteRenderer>();
            if (renderer != null && idleSprite != null)
            {
                renderer.sprite = idleSprite;
            }

            var petController = pet.GetComponent<PetController>();
            if (petController != null)
            {
                var serialized = new SerializedObject(petController);
                var controllerProperty = serialized.FindProperty("_movementController");
                if (controllerProperty != null)
                {
                    controllerProperty.objectReferenceValue = controller;
                }

                if (sideFramesFaceLeft.HasValue)
                {
                    var sideFramesFaceLeftProperty = serialized.FindProperty("_sideFramesFaceLeft");
                    if (sideFramesFaceLeftProperty != null)
                    {
                        sideFramesFaceLeftProperty.boolValue = sideFramesFaceLeft.Value;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(petController);
            }

            EnsureFreeWander(pet, boundsMin ?? new Vector2(-18f, -1.95f), boundsMax ?? new Vector2(18f, -1.65f));

            EditorUtility.SetDirty(animator);
            if (renderer != null) EditorUtility.SetDirty(renderer);
        }

        private static void EnsureFreeWander(GameObject pet, Vector2 boundsMin, Vector2 boundsMax)
        {
            var wander = pet.GetComponent<RandomWander>();
            if (wander == null)
            {
                wander = Undo.AddComponent<RandomWander>(pet);
            }

            var wanderSo = new SerializedObject(wander);
            SetVector2(wanderSo, "_boundsMin", boundsMin);
            SetVector2(wanderSo, "_boundsMax", boundsMax);
            SetFloat(wanderSo, "_moveSpeed", 1.2f);
            SetFloat(wanderSo, "_arrivalThreshold", 0.15f);
            SetFloat(wanderSo, "_minWaitSeconds", 2f);
            SetFloat(wanderSo, "_maxWaitSeconds", 5f);
            var horizontal = wanderSo.FindProperty("_horizontalOnly");
            if (horizontal != null) horizontal.boolValue = true;
            wanderSo.ApplyModifiedPropertiesWithoutUndo();

            var input = pet.GetComponent<PetPlayerInputController>();
            if (input == null)
            {
                input = Undo.AddComponent<PetPlayerInputController>(pet);
            }

            var inputSo = new SerializedObject(input);
            var prefer = inputSo.FindProperty("_preferControlOnEnable");
            if (prefer != null) prefer.boolValue = false;
            var inputHorizontal = inputSo.FindProperty("_horizontalOnly");
            if (inputHorizontal != null) inputHorizontal.boolValue = true;
            inputSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wander);
            EditorUtility.SetDirty(input);
        }

        private static void SetVector2(SerializedObject serialized, string propertyName, Vector2 value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.vector2Value = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static AnimationClip CreateOrUpdateClip(string path, IReadOnlyList<Sprite> frames, bool loop)
        {
            AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = FrameRate;
            var keyframes = new ObjectReferenceKeyframe[frames.Count];
            for (int index = 0; index < frames.Count; index++)
            {
                keyframes[index] = new ObjectReferenceKeyframe
                {
                    time = index / FrameRate,
                    value = frames[index]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                keyframes);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetStateMotion(AnimatorStateMachine stateMachine, string stateName, Motion motion)
        {
            AnimatorState state = FindState(stateMachine, stateName) ?? stateMachine.AddState(stateName);
            state.motion = motion;
            state.writeDefaultValues = true;
        }

        private static AnimatorState? FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state.name == stateName) return child.state;
            }
            return null;
        }

        private static void ClearTransitions(AnimatorStateMachine stateMachine)
        {
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (ChildAnimatorState child in stateMachine.states)
            {
                foreach (AnimatorStateTransition transition in child.state.transitions.ToArray())
                {
                    child.state.RemoveTransition(transition);
                }
            }
        }

        private static List<Sprite> LoadSprites(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return new List<Sprite>();
            return AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .OrderBy(sprite => ExtractTrailingNumber(sprite!.name))
                .Cast<Sprite>()
                .ToList();
        }

        private static int ExtractTrailingNumber(string name)
        {
            Match match = Regex.Match(name, @"(\d+)$");
            return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : int.MaxValue;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private readonly struct NamedFrames
        {
            public NamedFrames(string stateName, string folder, bool loop)
            {
                StateName = stateName;
                Folder = folder;
                Loop = loop;
            }

            public string StateName { get; }
            public string Folder { get; }
            public bool Loop { get; }
        }

        private sealed class PetAnimationSet
        {
            public PetAnimationSet(string petName, List<Sprite> idleFrames, List<Sprite> moveFrames, NamedFrames[] actions)
            {
                PetName = petName;
                IdleFrames = idleFrames;
                MoveFrames = moveFrames;
                Actions = actions;
            }

            public string PetName { get; }
            public List<Sprite> IdleFrames { get; }
            public List<Sprite> MoveFrames { get; }
            public NamedFrames[] Actions { get; }
        }
    }
}
#endif
