using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Reads and safely replaces the one JSON meta save in an injected directory.</summary>
    public sealed class SaveFileSerializer
    {
        public const string SaveFileName = "diveprotocol_save.json";
        public const string BackupFileName = "diveprotocol_save.backup.json";
        public const string TemporaryFileName = "diveprotocol_save.tmp.json";

        public SaveFileSerializer(string saveDirectory)
        {
            if (string.IsNullOrWhiteSpace(saveDirectory))
            {
                throw new ArgumentException("Save directory cannot be blank.", nameof(saveDirectory));
            }

            SaveDirectory = Path.GetFullPath(saveDirectory);
            SaveFilePath = Path.Combine(SaveDirectory, SaveFileName);
            BackupFilePath = Path.Combine(SaveDirectory, BackupFileName);
            TemporaryFilePath = Path.Combine(SaveDirectory, TemporaryFileName);
        }

        public string SaveDirectory { get; }
        public string SaveFilePath { get; }
        public string BackupFilePath { get; }
        public string TemporaryFilePath { get; }

        public MetaSaveData Load(string filePath)
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
            {
                throw new InvalidDataException($"Save file is not a JSON object: {filePath}");
            }

            var document = JsonUtility.FromJson<SaveDocument>(json);
            if (document == null)
            {
                throw new InvalidDataException($"Save JSON produced no data: {filePath}");
            }

            return MetaSaveData.Restore(
                document.SaveVersion,
                document.TotalCurrency,
                document.TotalRunsSettled,
                document.SuccessfulRuns,
                document.BossKills,
                document.RecentlyProcessedRunIds,
                document.LastUpdatedUtc);
        }

        public void Save(MetaSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            Directory.CreateDirectory(SaveDirectory);
            var json = JsonUtility.ToJson(new SaveDocument(saveData), true);

            using (var stream = new FileStream(TemporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            if (!File.Exists(TemporaryFilePath) || new FileInfo(TemporaryFilePath).Length == 0)
            {
                throw new IOException("Temporary save file was not written successfully.");
            }

            if (File.Exists(SaveFilePath))
            {
                File.Copy(SaveFilePath, BackupFilePath, true);
                File.Delete(SaveFilePath);
            }

            File.Move(TemporaryFilePath, SaveFilePath);
        }

        public string PreserveCorruptFile(string filePath, DateTime utcNow)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            var preservedPath = $"{filePath}.corrupt.{utcNow:yyyyMMddHHmmssfff}";
            File.Move(filePath, preservedPath);
            return preservedPath;
        }

        public void DeleteSaveFiles()
        {
            DeleteIfPresent(SaveFilePath);
            DeleteIfPresent(BackupFilePath);
            DeleteIfPresent(TemporaryFilePath);
        }

        private static void DeleteIfPresent(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        [Serializable]
        private sealed class SaveDocument
        {
            public int SaveVersion;
            public int TotalCurrency;
            public int TotalRunsSettled;
            public int SuccessfulRuns;
            public int BossKills;
            public List<string> RecentlyProcessedRunIds;
            public string LastUpdatedUtc;

            public SaveDocument()
            {
                RecentlyProcessedRunIds = new List<string>();
                LastUpdatedUtc = string.Empty;
            }

            public SaveDocument(MetaSaveData saveData)
            {
                SaveVersion = saveData.SaveVersion;
                TotalCurrency = saveData.TotalCurrency;
                TotalRunsSettled = saveData.TotalRunsSettled;
                SuccessfulRuns = saveData.SuccessfulRuns;
                BossKills = saveData.BossKills;
                RecentlyProcessedRunIds = new List<string>(saveData.RecentlyProcessedRunIds);
                LastUpdatedUtc = saveData.LastUpdatedUtc;
            }
        }
    }
}
