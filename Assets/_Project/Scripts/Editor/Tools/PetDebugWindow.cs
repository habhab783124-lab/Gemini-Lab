#if UNITY_EDITOR
#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using GeminiLab.Modules.Pet.Social;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.EditorTools
{
    public sealed class PetDebugWindow : EditorWindow
    {
        private float _friendshipTarget = 45f;
        private PersonalityVector _angel;
        private PersonalityVector _devil;
        private bool _angelLoaded;
        private bool _devilLoaded;

        [MenuItem("Tools/Gemini-Lab/Pet Debug")]
        public static void Open()
        {
            GetWindow<PetDebugWindow>("Pet Debug");
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("请进入 Play Mode 后使用。", MessageType.Info);
                return;
            }

            if (!DevMode.Active)
            {
                EditorGUILayout.HelpBox("当前不是 Dev Mode，调试窗口不可用。", MessageType.Warning);
                return;
            }

            if (!ServiceLocator.TryResolve(out IPetSocialService? social) || social is null)
            {
                EditorGUILayout.HelpBox("未找到 IPetSocialService。", MessageType.Warning);
                return;
            }

            if (!ServiceLocator.TryResolve(out IPersonalityEvolutionService? personality) || personality is null)
            {
                EditorGUILayout.HelpBox("未找到 IPersonalityEvolutionService。", MessageType.Warning);
                return;
            }

            DrawFriendship(social);
            EditorGUILayout.Space(12f);
            DrawPersonality(PetId.Angel, personality, ref _angel, ref _angelLoaded);
            EditorGUILayout.Space(12f);
            DrawPersonality(PetId.Devil, personality, ref _devil, ref _devilLoaded);
        }

        private void DrawFriendship(IPetSocialService social)
        {
            EditorGUILayout.LabelField("亲密度（Angel ↔ Devil 共享）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前亲密度", social.Friendship.ToString("F2"));
            _friendshipTarget = EditorGUILayout.FloatField("目标亲密度", _friendshipTarget);

            if (GUILayout.Button("设为目标亲密度"))
            {
                float delta = _friendshipTarget - social.Friendship;
                social.ApplySpecialEventFriendship(delta);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设为 30")) SetFriendship(social, 30f);
            if (GUILayout.Button("设为 45")) SetFriendship(social, 45f);
            if (GUILayout.Button("设为 80")) SetFriendship(social, 80f);
            EditorGUILayout.EndHorizontal();
        }

        private static void SetFriendship(IPetSocialService social, float target)
        {
            social.ApplySpecialEventFriendship(target - social.Friendship);
        }

        private static void DrawPersonality(
            PetId petId,
            IPersonalityEvolutionService personality,
            ref PersonalityVector editable,
            ref bool loaded)
        {
            EditorGUILayout.LabelField(petId == PetId.Angel ? "天使性格" : "恶魔性格", EditorStyles.boldLabel);

            if (!loaded)
            {
                editable = personality.GetMatrix(petId);
                loaded = true;
            }

            editable.Kindness = EditorGUILayout.Slider("善良", editable.Kindness, -1f, 1f);
            editable.Evilness = EditorGUILayout.Slider("邪恶", editable.Evilness, -1f, 1f);
            editable.Calmness = EditorGUILayout.Slider("冷静", editable.Calmness, -1f, 1f);
            editable.Bravery = EditorGUILayout.Slider("勇敢", editable.Bravery, -1f, 1f);
            editable.Shyness = EditorGUILayout.Slider("害羞", editable.Shyness, -1f, 1f);
            editable.Integrity = EditorGUILayout.Slider("正直", editable.Integrity, -1f, 1f);
            editable.Curiosity = EditorGUILayout.Slider("好奇心", editable.Curiosity, -1f, 1f);

            if (GUILayout.Button($"应用 {petId} 性格"))
            {
                personality.SetInitialMatrix(petId, editable.Clamp());
            }
        }
    }
}
#endif
