#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GeminiLab.Core;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.EditorTools
{
    public sealed class SaveDataDebugWindow : EditorWindow
    {
        private bool _developer = true;
        private bool _player;
        private bool _chat = true;
        private bool _progress = true;
        private Vector2 _scroll;
        private string _lastBackup = "";
        private string _message = "";

        [MenuItem("Tools/Gemini-Lab/Save Data Debug")]
        public static void Open()
        {
            var window = GetWindow<SaveDataDebugWindow>("存档调试");
            window.minSize = new Vector2(520, 440);
        }

        private void OnEnable()
        {
            _developer = DevMode.Active;
            _player = !DevMode.Active;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("存档清理", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(Application.persistentDataPath, EditorStyles.textField, GUILayout.Height(22));
            EditorGUILayout.HelpBox("先退出 Play，再清理。下次进入 Play 才会使用新档。公寓新手引导随存档一起重置；音量、语言和窗口设置保留。", MessageType.Info);
            bool busy = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating;
            using (new EditorGUI.DisabledScope(busy))
            {
                _developer = EditorGUILayout.ToggleLeft("开发存档 Saves-Dev（所有槽位、家具布局）", _developer);
                _player = EditorGUILayout.ToggleLeft("玩家存档 Saves（所有槽位、家具布局）", _player);
                _chat = EditorGUILayout.ToggleLeft("聊天记录 chat_history.json（两种模式共用）", _chat);
                _progress = EditorGUILayout.ToggleLeft("全局进度：序章、跨日记录、调试日期、许愿记录（两种模式共用）", _progress);
                try
                {
                    var files = SaveDataDebugStorage.Preview(Application.persistentDataPath, _developer, _player, _chat);
                    EditorGUILayout.LabelField($"待清理文件：{files.Length}");
                    _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(90));
                    foreach (string file in files) EditorGUILayout.LabelField(file);
                    EditorGUILayout.EndScrollView();
                    using (new EditorGUI.DisabledScope(files.Length == 0 && !_progress))
                    {
                        if (GUILayout.Button("备份并清理所选存档", GUILayout.Height(30))) Clear(false, files.Length);
                    }
                    if (GUILayout.Button("仅重置序章播放标记")) Clear(true, 0);
                }
                catch (Exception ex) { EditorGUILayout.HelpBox(ex.Message, MessageType.Error); }
            }
            if (busy) EditorGUILayout.HelpBox("运行、切换 Play 或编译/导入时不能清理存档。", MessageType.Warning);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            if (GUILayout.Button("打开存档目录")) EditorUtility.RevealInFinder(Application.persistentDataPath);
            if (!string.IsNullOrEmpty(_lastBackup) && GUILayout.Button("打开本次备份")) EditorUtility.RevealInFinder(_lastBackup);
        }

        private void Clear(bool prologueOnly, int fileCount)
        {
            string detail = prologueOnly ? "仅重置序章播放标记，保留全部存档。" : $"清理预览中的 {fileCount} 个文件。" + (_progress ? "同时重置勾选的全局进度。" : "保留全局进度。");
            if (!EditorUtility.DisplayDialog("确认清理", detail + "\n\n文件及原进度键会先备份到存档目录内的 SaveDebugBackups。", "备份并清理", "取消")) return;
            try
            {
                _lastBackup = SaveDataDebugStorage.Clear(Application.persistentDataPath,
                    !prologueOnly && _developer, !prologueOnly && _player, !prologueOnly && _chat,
                    !prologueOnly && _progress, prologueOnly);
                _message = "清理完成。下次从 Boot 启动可验证开局流程。\n备份：" + _lastBackup;
                Debug.Log("[SaveDataDebug] 清理完成，备份：" + _lastBackup);
            }
            catch (Exception ex)
            {
                _message = "清理未完成：" + ex.Message;
                Debug.LogException(ex);
            }
        }
    }

    /// <summary>仅处理项目已知的存档文件；原文件移入备份，发生异常时移回。</summary>
    public static class SaveDataDebugStorage
    {
        private static readonly Regex SlotFile = new("^[A-Za-z0-9_-]+\\.sav(?:\\.tmp|\\.bak)?$", RegexOptions.IgnoreCase);
        private static readonly string[] IntKeys = { "HasPlayedPrologue", "GeminiLab.Debug.ClockOffsetDays" };
        private static readonly string[] StringKeys = { "GeminiLab.DailyReset.LastDate", "geminilab.worldmap.wishes.v1" };

        [Serializable] public sealed class Preference
        {
            public string key = "";
            public bool exists;
            public bool isInt;
            public int intValue;
            public string stringValue = "";
        }

        [Serializable] public sealed class BackupManifest
        {
            public string sourceRoot = "";
            public string createdUtc = "";
            public string status = "prepared";
            public string[] files = Array.Empty<string>();
            public Preference[] preferences = Array.Empty<Preference>();
        }

        public static string[] Preview(string root, bool developer, bool player, bool chat)
        {
            root = ValidateRoot(root);
            var files = new List<string>();
            foreach (string directory in new[] { developer ? "Saves-Dev" : "", player ? "Saves" : "" })
            {
                if (directory.Length == 0) continue;
                string path = SafePath(root, directory);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path).OrderBy(x => x, StringComparer.Ordinal))
                {
                    if (!SlotFile.IsMatch(Path.GetFileName(file))) continue;
                    string relative = directory + "/" + Path.GetFileName(file);
                    SafePath(root, relative);
                    files.Add(relative);
                }
            }
            if (chat && File.Exists(SafePath(root, "chat_history.json"))) files.Add("chat_history.json");
            return files.ToArray();
        }

        public static string Clear(string root, bool developer, bool player, bool chat, bool progress, bool prologueOnly = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("请退出 Play 并等待编译和导入结束。");
            root = ValidateRoot(root);
            var manifest = new BackupManifest
            {
                sourceRoot = root,
                createdUtc = DateTime.UtcNow.ToString("o"),
                files = Preview(root, developer && !prologueOnly, player && !prologueOnly, chat && !prologueOnly),
                preferences = CapturePreferences(progress && !prologueOnly, prologueOnly)
            };
            string backup = SafePath(root, "SaveDebugBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup);
            string manifestPath = Path.Combine(backup, "manifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            var moved = new List<string>();
            try
            {
                foreach (string relative in manifest.files)
                {
                    string source = SafePath(root, relative);
                    string destination = SafePath(backup, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Move(source, destination);
                    moved.Add(relative);
                }
                foreach (var pref in manifest.preferences) PlayerPrefs.DeleteKey(pref.key);
                if (manifest.preferences.Length > 0) PlayerPrefs.Save();
                manifest.status = "completed";
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
                return backup;
            }
            catch (Exception error)
            {
                // 回滚不覆盖同时出现的新文件；即使回滚失败，原文件仍在备份目录中。
                var errors = new List<Exception> { error };
                foreach (string relative in moved.AsEnumerable().Reverse())
                {
                    try { File.Move(SafePath(backup, relative), SafePath(root, relative)); }
                    catch (Exception rollbackError) { errors.Add(rollbackError); }
                }
                try
                {
                    foreach (var pref in manifest.preferences)
                    {
                        if (!pref.exists) PlayerPrefs.DeleteKey(pref.key);
                        else if (pref.isInt) PlayerPrefs.SetInt(pref.key, pref.intValue);
                        else PlayerPrefs.SetString(pref.key, pref.stringValue);
                    }
                    if (manifest.preferences.Length > 0) PlayerPrefs.Save();
                }
                catch (Exception rollbackError) { errors.Add(rollbackError); }
                throw new AggregateException("清理失败，已尝试恢复原数据。备份位置：" + backup, errors);
            }
        }

        private static Preference[] CapturePreferences(bool progress, bool prologueOnly)
        {
            var result = new List<Preference>();
            foreach (string key in prologueOnly ? new[] { "HasPlayedPrologue" } : progress ? IntKeys : Array.Empty<string>())
                result.Add(new Preference { key = key, exists = PlayerPrefs.HasKey(key), isInt = true, intValue = PlayerPrefs.GetInt(key) });
            if (progress)
                foreach (string key in StringKeys)
                    result.Add(new Preference { key = key, exists = PlayerPrefs.HasKey(key), stringValue = PlayerPrefs.GetString(key) });
            return result.ToArray();
        }

        private static string ValidateRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root)) throw new ArgumentException("存档根目录必须是绝对路径。");
            string full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(full, Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("不能将磁盘根目录作为存档目录。");
            RejectLinks(full);
            return full;
        }

        private static string SafePath(string root, string relative)
        {
            string full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("目标不在存档目录内。");
            RejectLinks(full);
            return full;
        }

        private static void RejectLinks(string path)
        {
            for (string? current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("为避免越界，不处理符号链接或目录联接：" + current);
        }
    }
}
#endif
