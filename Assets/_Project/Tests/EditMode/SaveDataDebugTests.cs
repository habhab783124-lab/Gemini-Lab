#if UNITY_EDITOR
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class SaveDataDebugTests
    {
        private string _root = "";
        private Type _storage = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "GeminiLabSaveDebugTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _storage = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GeminiLab.EditorTools.SaveDataDebugStorage"))
                .First(t => t != null)!;
        }

        [TearDown]
        public void TearDown()
        {
            string expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GeminiLabSaveDebugTests")) + Path.DirectorySeparatorChar;
            Assert.That(Path.GetFullPath(_root).StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase), Is.True);
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void Preview_OnlyIncludesKnownFilesInSelectedDirectories()
        {
            Write("Saves-Dev/autosave.sav");
            Write("Saves-Dev/autosave.sav.tmp");
            Write("Saves-Dev/autosave.sav.bak");
            Write("Saves-Dev/readme.txt");
            Write("Saves-Dev/nested/slot1.sav");
            Write("Saves/slot1.sav");
            Write("chat_history.json");
            Write("SaveDebugBackups/old/Saves-Dev/autosave.sav");
            var paths = (string[])Call("Preview", _root, true, false, false);
            Assert.That(paths, Is.EquivalentTo(new[] { "Saves-Dev/autosave.sav", "Saves-Dev/autosave.sav.tmp", "Saves-Dev/autosave.sav.bak" }));
        }

        [Test]
        public void Clear_MovesExactBytesToBackupAndPreservesOtherData()
        {
            byte[] bytes = { 0, 1, 255, 34, 67 };
            Write("Saves-Dev/autosave.sav");
            File.WriteAllBytes(Path.Combine(_root, "Saves-Dev/autosave.sav"), bytes);
            Write("Saves/slot1.sav", "player");
            Write("chat_history.json", "chat");
            Write("Saves-Dev/notes.txt", "keep");
            string backup = Clear(true, false, false);
            Assert.That(File.Exists(Path.Combine(_root, "Saves-Dev/autosave.sav")), Is.False);
            Assert.That(File.ReadAllBytes(Path.Combine(backup, "Saves-Dev/autosave.sav")), Is.EqualTo(bytes));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves/slot1.sav")), Is.EqualTo("player"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "chat_history.json")), Is.EqualTo("chat"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves-Dev/notes.txt")), Is.EqualTo("keep"));
            Assert.That(File.ReadAllText(Path.Combine(backup, "manifest.json")), Does.Contain("completed"));
        }

        [Test]
        public void ChatOnly_DoesNotTouchSaveSlots()
        {
            Write("chat_history.json", "chat");
            Write("Saves-Dev/furniture_layout.sav", "layout");
            string backup = Clear(false, false, true);
            Assert.That(File.ReadAllText(Path.Combine(backup, "chat_history.json")), Is.EqualTo("chat"));
            Assert.That(File.Exists(Path.Combine(_root, "chat_history.json")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves-Dev/furniture_layout.sav")), Is.EqualTo("layout"));
        }

        [Test]
        public void BackupCreationFailure_LeavesSourceUntouched()
        {
            Write("Saves-Dev/autosave.sav", "original");
            Write("SaveDebugBackups", "blocks directory creation");
            Assert.Throws<TargetInvocationException>(() => Clear(true, false, false));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves-Dev/autosave.sav")), Is.EqualTo("original"));
        }

        [Test]
        [Platform("Win")]
        public void MoveFailure_RollsBackEarlierFiles()
        {
            Write("Saves-Dev/a.sav", "a");
            Write("Saves-Dev/z.sav", "z");
            using (new FileStream(Path.Combine(_root, "Saves-Dev/z.sav"), FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.Throws<TargetInvocationException>(() => Clear(true, false, false));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves-Dev/a.sav")), Is.EqualTo("a"));
            Assert.That(File.ReadAllText(Path.Combine(_root, "Saves-Dev/z.sav")), Is.EqualTo("z"));
        }

        [Test]
        public void RepeatedClear_PreservesPriorBackup()
        {
            Write("Saves-Dev/autosave.sav", "first");
            string first = Clear(true, true, true);
            string second = Clear(true, true, true);
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(File.ReadAllText(Path.Combine(first, "Saves-Dev/autosave.sav")), Is.EqualTo("first"));
            Assert.That((string[])Call("Preview", _root, true, true, true), Is.Empty);
        }

        [Test]
        public void Preview_RejectsRelativeAndDriveRoots()
        {
            Assert.Throws<TargetInvocationException>(() => Call("Preview", "relative", true, true, true));
            Assert.Throws<TargetInvocationException>(() => Call("Preview", Path.GetPathRoot(_root)!, true, true, true));
        }

        [Test]
        public void PreferenceScope_ContainsOnlyKnownProgressKeys()
        {
            var capture = _storage.GetMethod("CapturePreferences", BindingFlags.NonPublic | BindingFlags.Static)!;
            var prefs = (Array)capture.Invoke(null, new object[] { true, false })!;
            string[] keys = prefs.Cast<object>().Select(p => (string)p.GetType().GetField("key")!.GetValue(p)!).ToArray();
            Assert.That(keys, Is.EquivalentTo(new[] { "HasPlayedPrologue", "GeminiLab.Debug.ClockOffsetDays", "GeminiLab.DailyReset.LastDate", "geminilab.worldmap.wishes.v1" }));
            var prologue = (Array)capture.Invoke(null, new object[] { false, true })!;
            Assert.That(prologue.Length, Is.EqualTo(1));
            Assert.That(prologue.GetValue(0)!.GetType().GetField("key")!.GetValue(prologue.GetValue(0)), Is.EqualTo("HasPlayedPrologue"));
        }

        private string Clear(bool developer, bool player, bool chat) =>
            (string)Call("Clear", _root, developer, player, chat, false, false);

        private object Call(string name, params object[] args) => _storage.GetMethod(name)!.Invoke(null, args)!;

        private void Write(string relative, string content = "data")
        {
            string path = Path.Combine(_root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }
    }
}
#endif
