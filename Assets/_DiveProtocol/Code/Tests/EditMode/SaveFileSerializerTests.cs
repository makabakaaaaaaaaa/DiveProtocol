using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class SaveFileSerializerTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), $"DiveProtocolSerializerTests_{Guid.NewGuid():N}");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void FirstInitializationCreatesOnlyOneOfficialSave()
        {
            var manager = new SaveManager(_directory);
            manager.Initialize();

            Assert.That(File.Exists(Path.Combine(_directory, SaveFileSerializer.SaveFileName)), Is.True);
            Assert.That(Directory.GetFiles(_directory, "*slot*", SearchOption.TopDirectoryOnly), Is.Empty);
            Assert.That(File.Exists(Path.Combine(_directory, SaveFileSerializer.TemporaryFileName)), Is.False);
        }

        [Test]
        public void SaveLoadRoundTripPreservesMetaFieldsAndVersion()
        {
            var serializer = new SaveFileSerializer(_directory);
            var data = MetaSaveData.CreateDefault(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            data.ApplySettlement("run-a", 25, true, true, DateTime.UtcNow);
            serializer.Save(data);

            var loaded = serializer.Load(serializer.SaveFilePath);
            var json = File.ReadAllText(serializer.SaveFilePath);

            Assert.That(loaded.SaveVersion, Is.EqualTo(SaveVersion.Current));
            Assert.That(loaded.TotalCurrency, Is.EqualTo(25));
            Assert.That(loaded.TotalRunsSettled, Is.EqualTo(1));
            Assert.That(loaded.SuccessfulRuns, Is.EqualTo(1));
            Assert.That(loaded.BossKills, Is.EqualTo(1));
            StringAssert.Contains($"\"SaveVersion\": {SaveVersion.Current}", json);
            Assert.That(File.Exists(serializer.TemporaryFilePath), Is.False);
        }

        [Test]
        public void DeleteSaveFilesRemovesOfficialBackupAndTemporaryFiles()
        {
            var serializer = new SaveFileSerializer(_directory);
            Directory.CreateDirectory(_directory);
            File.WriteAllText(serializer.SaveFilePath, "primary");
            File.WriteAllText(serializer.BackupFilePath, "backup");
            File.WriteAllText(serializer.TemporaryFilePath, "temporary");

            serializer.DeleteSaveFiles();

            Assert.That(File.Exists(serializer.SaveFilePath), Is.False);
            Assert.That(File.Exists(serializer.BackupFilePath), Is.False);
            Assert.That(File.Exists(serializer.TemporaryFilePath), Is.False);
        }

        [Test]
        public void NewerVersionIsLoadedReadOnlyAndNotSilentlyOverwritten()
        {
            Directory.CreateDirectory(_directory);
            var path = Path.Combine(_directory, SaveFileSerializer.SaveFileName);
            File.WriteAllText(path, "{\"SaveVersion\":999,\"TotalCurrency\":77}");
            var original = File.ReadAllText(path);
            var manager = new SaveManager(_directory);
            manager.Initialize();
            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[Save\] Meta progression is unavailable or read-only\."));

            Assert.That(manager.CurrentMeta.SaveVersion, Is.EqualTo(999));
            Assert.That(manager.AddTestCurrency(10), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        }
    }
}
