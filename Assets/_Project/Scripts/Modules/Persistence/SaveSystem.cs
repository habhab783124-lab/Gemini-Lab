#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// Save-system contract for module-level persistence.
    /// </summary>
    public interface ISaveSystem
    {
        /// <summary>
        /// Saves data into the specified slot.
        /// </summary>
        Task SaveAsync<T>(string slot, T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves data into the specified slot synchronously.
        /// </summary>
        void SaveNow<T>(string slot, T data);

        /// <summary>
        /// Loads data from a slot, or returns null when slot doesn't exist.
        /// </summary>
        Task<T?> LoadAsync<T>(string slot, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Deletes an existing slot.
        /// </summary>
        Task DeleteSlotAsync(string slot, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// JSON persistence with optional AES placeholder encryption strategy.
    /// </summary>
    public sealed class SaveSystem : ISaveSystem
    {
        private static readonly Regex SlotRegex = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SlotLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _saveRootPath;
        private readonly IStringEncryptionStrategy _encryption;

        public SaveSystem(IStringEncryptionStrategy? encryption = null, string? saveRootPath = null)
        {
            _encryption = encryption ?? new PlainTextEncryptionStrategy();
            _saveRootPath = Path.GetFullPath(saveRootPath ?? Path.Combine(Application.persistentDataPath, "Saves"));
            Directory.CreateDirectory(_saveRootPath);
        }

        public async Task SaveAsync<T>(string slot, T data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Slot cannot be null or empty.", nameof(slot));
            }

            string path = ResolveSlotPath(slot);
            SemaphoreSlim slotLock = GetSlotLock(path);
            await slotLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WriteSlotAsync(path, data, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                slotLock.Release();
            }
        }

        public void SaveNow<T>(string slot, T data)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Slot cannot be null or empty.", nameof(slot));
            }

            string path = ResolveSlotPath(slot);
            SemaphoreSlim slotLock = GetSlotLock(path);
            slotLock.Wait();
            try
            {
                WriteSlotSync(path, data);
            }
            finally
            {
                slotLock.Release();
            }
        }

        public async Task<T?> LoadAsync<T>(string slot, CancellationToken cancellationToken = default) where T : class
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Slot cannot be null or empty.", nameof(slot));
            }

            string path = ResolveSlotPath(slot);
            SemaphoreSlim slotLock = GetSlotLock(path);
            await slotLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string encoded = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                string json = _encryption.Decrypt(encoded);
                SaveEnvelope<T>? envelope = JsonUtility.FromJson<SaveEnvelope<T>>(json);
                return envelope?.Payload;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to load slot '{slot}': {ex.Message}");
                return null;
            }
            finally
            {
                slotLock.Release();
            }
        }

        public async Task DeleteSlotAsync(string slot, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Slot cannot be null or empty.", nameof(slot));
            }

            string path = ResolveSlotPath(slot);
            SemaphoreSlim slotLock = GetSlotLock(path);
            await slotLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            finally
            {
                slotLock.Release();
            }
        }

        private string ResolveSlotPath(string slot)
        {
            if (!SlotRegex.IsMatch(slot))
            {
                throw new ArgumentException("Slot can only contain letters, numbers, underscore, and dash.", nameof(slot));
            }

            string fullPath = Path.GetFullPath(Path.Combine(_saveRootPath, $"{slot}.sav"));
            string rootedBase = _saveRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootedBase, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Resolved slot path is outside save root.");
            }

            return fullPath;
        }

        private static SemaphoreSlim GetSlotLock(string slotPath)
        {
            return SlotLocks.GetOrAdd(slotPath, static _ => new SemaphoreSlim(1, 1));
        }

        private async Task WriteSlotAsync<T>(string path, T data, CancellationToken cancellationToken)
        {
            string tempPath = path + ".tmp";
            SaveEnvelope<T> envelope = new()
            {
                SchemaVersion = 1,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Payload = data
            };

            string json = JsonUtility.ToJson(envelope, prettyPrint: false);
            string encoded = _encryption.Encrypt(json);
            await File.WriteAllTextAsync(tempPath, encoded, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            CommitTempFile(path, tempPath);
        }

        private void WriteSlotSync<T>(string path, T data)
        {
            string tempPath = path + ".tmp";
            SaveEnvelope<T> envelope = new()
            {
                SchemaVersion = 1,
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Payload = data
            };

            string json = JsonUtility.ToJson(envelope, prettyPrint: false);
            string encoded = _encryption.Encrypt(json);
            File.WriteAllText(tempPath, encoded, Encoding.UTF8);
            CommitTempFile(path, tempPath);
        }

        private static void CommitTempFile(string path, string tempPath)
        {
            if (File.Exists(path))
            {
                string backupPath = path + ".bak";
                File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
    }

    /// <summary>
    /// Encryption strategy abstraction to avoid hard-coding keys in SaveSystem.
    /// </summary>
    public interface IStringEncryptionStrategy
    {
        string Encrypt(string plainText);

        string Decrypt(string cipherText);
    }

    /// <summary>
    /// Default no-op encryption strategy.
    /// </summary>
    public sealed class PlainTextEncryptionStrategy : IStringEncryptionStrategy
    {
        public string Encrypt(string plainText) => plainText;

        public string Decrypt(string cipherText) => cipherText;
    }

    /// <summary>
    /// AES-CBC placeholder strategy for phase-1 encrypted save experiments.
    /// </summary>
    public sealed class AesEncryptionStrategyPlaceholder : IStringEncryptionStrategy
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptionStrategyPlaceholder(byte[] key, byte[] iv)
        {
            if (key is null || key.Length is not (16 or 24 or 32))
            {
                throw new ArgumentException("AES key must be 16, 24, or 32 bytes.", nameof(key));
            }

            if (iv is null || iv.Length != 16)
            {
                throw new ArgumentException("AES IV must be 16 bytes.", nameof(iv));
            }

            _key = key;
            _iv = iv;
        }

        public string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] inputBytes = Convert.FromBase64String(cipherText);
            byte[] decrypted = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
    }

    [Serializable]
    internal sealed class SaveEnvelope<T>
    {
        public int SchemaVersion = 1;
        public string SavedAtUtc = string.Empty;
        public T? Payload;
    }
}
