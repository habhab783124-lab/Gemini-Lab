#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using System.Reflection;
using GeminiLab.Core;
using GeminiLab.Modules.Pet.Social;
using GeminiLab.Modules.RoomRelic;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.EditorTools
{
    public sealed class RoomRelicDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Gemini-Lab/Room Relic Debug")]
        public static void Open()
        {
            GetWindow<RoomRelicDebugWindow>("Room Relic Debug");
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

            if (!ServiceLocator.TryResolve(out IRoomRelicService? relic) || relic is null)
            {
                EditorGUILayout.HelpBox("未找到 IRoomRelicService。", MessageType.Warning);
                return;
            }

            DrawFriendship();
            EditorGUILayout.Space(8f);
            DrawState(relic);
            EditorGUILayout.Space(8f);
            DrawRoll(relic);
        }

        private static void DrawFriendship()
        {
            if (!ServiceLocator.TryResolve(out IPetSocialService? social) || social is null)
            {
                return;
            }

            EditorGUILayout.LabelField("好友度（解锁遗物/赠礼）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前好友度", social.Friendship.ToString("F2"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("设为 0")) social.ApplySpecialEventFriendship(-social.Friendship);
            if (GUILayout.Button("设为 45")) social.ApplySpecialEventFriendship(45f - social.Friendship);
            if (GUILayout.Button("设为 80")) social.ApplySpecialEventFriendship(80f - social.Friendship);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawState(IRoomRelicService relic)
        {
            EditorGUILayout.LabelField("当前掉落状态", EditorStyles.boldLabel);
            foreach (RoomId room in new[] { RoomId.AngelRoom, RoomId.DevilRoom })
            {
                RoomRelicSnapshot snap = relic.GetSnapshot(room);
                EditorGUILayout.LabelField(
                    $"{room}: 纸条={snap.CurrentNote?.id ?? "-"}  遗物={snap.CurrentRelic?.displayName ?? "-"}  赠礼={snap.PlacedGifts.Count}件");
            }
        }

        private static void DrawRoll(IRoomRelicService relic)
        {
            EditorGUILayout.LabelField("强制触发掉落（重置每日判定后进入房间）", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("触发 Angel 房间"))
            {
                ResetDailyRoll(relic);
                relic.ProcessRoomEntry(RoomId.AngelRoom);
            }

            if (GUILayout.Button("触发 Devil 房间"))
            {
                ResetDailyRoll(relic);
                relic.ProcessRoomEntry(RoomId.DevilRoom);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("遗物需好友度≥45，赠礼需≥80；掉落仍保留纸条50%/遗物50%/赠礼15%概率。", MessageType.Info);
        }

        private static void ResetDailyRoll(IRoomRelicService relic)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            System.Type type = typeof(RoomRelicService);

            if (type.GetField("_lastEntryDateByRoom", flags)?.GetValue(relic) is Dictionary<RoomId, string> entryDates)
            {
                entryDates[RoomId.AngelRoom] = string.Empty;
                entryDates[RoomId.DevilRoom] = string.Empty;
            }

            if (type.GetField("_states", flags)?.GetValue(relic) is Dictionary<RoomId, RoomRollState> states)
            {
                foreach (RoomRollState state in states.Values)
                {
                    state.lastNoteRollDateIso = string.Empty;
                    state.lastRelicRollDateIso = string.Empty;
                    state.lastGiftRollDateIso = string.Empty;
                }
            }
        }
    }
}
#endif
