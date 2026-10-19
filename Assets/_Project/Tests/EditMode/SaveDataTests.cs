using System.IO;
using MathRunner.Save;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEngine;

namespace MathRunner.Tests
{
    public class SaveDataTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "mathrunner-test-" + System.Guid.NewGuid() + ".json");
            SaveSystem.UseFile(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path)) File.Delete(_path);
            SaveSystem.UseFile(null);
        }

        [Test]
        public void NewSaveUnlocksOnlyLevelOne()
        {
            var data = new SaveData();
            Assert.That(data.IsUnlocked(1), Is.True);
            Assert.That(data.IsUnlocked(2), Is.False);
            Assert.That(data.GetRecord(1), Is.Null);
        }

        [Test]
        public void CompletingALevelUnlocksTheNextAndKeepsTheBest()
        {
            var data = new SaveData();
            Assert.That(data.RecordCompletion(1, 800, 2), Is.True, "First completion is a new best");
            Assert.That(data.IsUnlocked(2), Is.True);
            Assert.That(data.RecordCompletion(1, 500, 3), Is.False, "Lower score is not a new best");
            var record = data.GetRecord(1);
            Assert.That(record.BestScore, Is.EqualTo(800));
            Assert.That(record.BestStars, Is.EqualTo(3), "Stars keep the best ever, independent of score");
        }

        [Test]
        public void ReplayingAnEarlierLevelDoesNotLockLaterOnes()
        {
            var data = new SaveData();
            data.RecordCompletion(1, 100, 1);
            data.RecordCompletion(2, 100, 1);
            data.RecordCompletion(1, 200, 1);
            Assert.That(data.HighestUnlockedLevel, Is.EqualTo(3));
        }

        [Test]
        public void SavedDataSurvivesReloadingFromDisk()
        {
            SaveSystem.Data.RecordCompletion(3, 1234, 2);
            SaveSystem.Save();
            SaveSystem.UseFile(_path);
            Assert.That(SaveSystem.Data.HighestUnlockedLevel, Is.EqualTo(4));
            Assert.That(SaveSystem.Data.GetRecord(3).BestScore, Is.EqualTo(1234));
            Assert.That(SaveSystem.Data.Version, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void CorruptSaveFileFallsBackToAFreshSave()
        {
            File.WriteAllText(_path, "{ this is not json");
            SaveSystem.UseFile(_path);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("could not be read"));
            Assert.That(SaveSystem.Data.HighestUnlockedLevel, Is.EqualTo(1));
        }
    }

    public class StarRatingTests
    {
        [Test]
        public void FlawlessRunEarnsThreeStars()
        {
            Assert.That(StarRating.For(8, 8, 3, 3), Is.EqualTo(3));
        }

        [Test]
        public void LosingALifeToAHazardCostsTheThirdStar()
        {
            Assert.That(StarRating.For(8, 8, 2, 3), Is.EqualTo(2));
        }

        [Test]
        public void SeventyFivePercentCorrectEarnsTwoStars()
        {
            Assert.That(StarRating.For(6, 8, 1, 3), Is.EqualTo(2));
            Assert.That(StarRating.For(5, 8, 1, 3), Is.EqualTo(1));
        }
    }
}
