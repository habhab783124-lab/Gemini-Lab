#if UNITY_EDITOR
#nullable enable
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

namespace GeminiLab.EditorTools.Pet
{
    /// <summary>
    /// Builds pet movement clips/controller from naming convention:
    /// Pet_Angel_Move_{Front|Back|Side}_0001...
    /// </summary>
    public static class PetMoveAnimationSetupEditor
    {
        private const string SpriteFolder = "Assets/_Project/Art/Sprites/Pet/Frames/Move";
        private const string AnimationFolder = "Assets/_Project/Animations/Pet";
        private const string FrontClipPath = AnimationFolder + "/Pet_Angel_Move_Front.anim";
        private const string BackClipPath = AnimationFolder + "/Pet_Angel_Move_Back.anim";
        private const string SideClipPath = AnimationFolder + "/Pet_Angel_Move_Side.anim";
        private const string ControllerPath = AnimationFolder + "/Pet_Angel.controller";
        private const float DefaultFps = 12f;

        [MenuItem("Tools/GeminiLab/Pet/Setup Move Animations")]
        public static void SetupMoveAnimations()
        {
            try
            {
                EnsureFolder(AnimationFolder);

                List<Sprite> frontSprites = LoadSpritesByPrefix("Pet_Angel_Move_Front_");
                List<Sprite> backSprites = LoadSpritesByPrefix("Pet_Angel_Move_Back_");
                List<Sprite> sideSprites = LoadSpritesByPrefix("Pet_Angel_Move_Side_");

                if (frontSprites.Count == 0 || backSprites.Count == 0 || sideSprites.Count == 0)
                {
                    Debug.LogError($"[PetAnimSetup] Missing sequence frames. Front={frontSprites.Count}, Back={backSprites.Count}, Side={sideSprites.Count}");
                    return;
                }

                AnimationClip frontClip = CreateOrUpdateSpriteClip(FrontClipPath, frontSprites, DefaultFps);
                AnimationClip backClip = CreateOrUpdateSpriteClip(BackClipPath, backSprites, DefaultFps);
                AnimationClip sideClip = CreateOrUpdateSpriteClip(SideClipPath, sideSprites, DefaultFps);

                AnimatorController controller = CreateOrUpdateController(ControllerPath, frontClip, backClip, sideClip);
                int assigned = BindControllerToPetControllers(controller);
                EditorSceneManager.MarkAllScenesDirty();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[PetAnimSetup] Completed. Clips updated: 3, controller: {controller.name}, animators assigned/updated: {assigned}.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static int BindControllerToPetControllers(AnimatorController controller)
        {
            int assigned = 0;
            PetController[] pets = UnityEngine.Object.FindObjectsByType<PetController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pets.Length; i++)
            {
                PetController pet = pets[i];
                if (pet == null)
                {
                    continue;
                }

                bool hasAnimator = pet.TryGetComponent(out Animator animator);
                if (!hasAnimator || animator == null)
                {
                    animator = pet.gameObject.AddComponent<Animator>();
                }

                SerializedObject petSerialized = new(pet);
                SerializedProperty? controllerProperty = petSerialized.FindProperty("_movementController");
                if (controllerProperty is not null && controllerProperty.objectReferenceValue != controller)
                {
                    controllerProperty.objectReferenceValue = controller;
                    petSerialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pet);
                }

                if (animator.runtimeAnimatorController != controller)
                {
                    animator.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(animator);
                    assigned++;
                }
            }

            return assigned;
        }

        private static AnimatorController CreateOrUpdateController(string path, AnimationClip front, AnimationClip back, AnimationClip side)
        {
            AnimatorController? controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller is null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveDir", AnimatorControllerParameterType.Int);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState frontState = GetOrCreateState(sm, "Move_Front", front);
            AnimatorState backState = GetOrCreateState(sm, "Move_Back", back);
            AnimatorState sideState = GetOrCreateState(sm, "Move_Side", side);
            sm.defaultState = frontState;

            ClearAnyStateTransitions(sm);
            AddDirectionTransition(sm, frontState, dir: 0);
            AddDirectionTransition(sm, backState, dir: 1);
            AddDirectionTransition(sm, sideState, dir: 2);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddDirectionTransition(AnimatorStateMachine sm, AnimatorState destination, int dir)
        {
            AnimatorStateTransition transition = sm.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            transition.AddCondition(AnimatorConditionMode.Equals, dir, "MoveDir");
        }

        private static void ClearAnyStateTransitions(AnimatorStateMachine sm)
        {
            AnimatorStateTransition[] transitions = sm.anyStateTransitions.ToArray();
            for (int i = 0; i < transitions.Length; i++)
            {
                sm.RemoveAnyStateTransition(transitions[i]);
            }
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName, Motion motion)
        {
            ChildAnimatorState[] states = sm.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state.name != stateName)
                {
                    continue;
                }

                states[i].state.motion = motion;
                states[i].state.writeDefaultValues = true;
                return states[i].state;
            }

            AnimatorState state = sm.AddState(stateName);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string assetPath, IReadOnlyList<Sprite> sprites, float fps)
        {
            AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip is null)
            {
                clip = new AnimationClip
                {
                    frameRate = fps,
                    name = Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(clip, assetPath);
            }

            clip.frameRate = fps;

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty,
                typeof(SpriteRenderer),
                "m_Sprite");

            ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                frames[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static List<Sprite> LoadSpritesByPrefix(string prefix)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {prefix}", new[] { SpriteFolder });
            Regex suffixRegex = new(@"_(\d+)$", RegexOptions.Compiled);

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite is not null && sprite.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(sprite => ExtractOrder(sprite!.name, suffixRegex))
                .Cast<Sprite>()
                .ToList();
        }

        private static int ExtractOrder(string spriteName, Regex suffixRegex)
        {
            Match match = suffixRegex.Match(spriteName);
            if (!match.Success)
            {
                return int.MaxValue;
            }

            return int.TryParse(match.Groups[1].Value, out int order) ? order : int.MaxValue;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
