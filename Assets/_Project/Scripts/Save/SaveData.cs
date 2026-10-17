using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Save
{
    [Serializable]
    public class LevelRecord
    {
        [SerializeField] private int _levelNumber;
        [SerializeField] private int _bestScore;
        [SerializeField] private int _bestStars;

        public LevelRecord(int levelNumber)
        {
            _levelNumber = levelNumber;
        }

        public int LevelNumber => _levelNumber;
        public int BestScore => _bestScore;
        public int BestStars => _bestStars;

        // Returns true if the score beats the previous best.
        public bool Apply(int score, int stars)
        {
            bool newBest = score > _bestScore;
            _bestScore = Mathf.Max(_bestScore, score);
            _bestStars = Mathf.Max(_bestStars, stars);
            return newBest;
        }
    }

    // Everything persisted between sessions. Serialized with JsonUtility; bump Version when the layout changes.
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private int _highestUnlockedLevel = 1;
        [SerializeField] private List<LevelRecord> _levels = new List<LevelRecord>();

        public int Version => _version;
        public int HighestUnlockedLevel => _highestUnlockedLevel;

        public bool IsUnlocked(int levelNumber) => levelNumber <= _highestUnlockedLevel;

        // Null if the level has never been completed.
        public LevelRecord GetRecord(int levelNumber) => _levels.Find(r => r.LevelNumber == levelNumber);

        // Records a finished level, unlocks the next one and returns true if the score is a new best.
        public bool RecordCompletion(int levelNumber, int score, int stars)
        {
            var record = GetRecord(levelNumber);
            if (record == null)
            {
                record = new LevelRecord(levelNumber);
                _levels.Add(record);
            }
            if (levelNumber + 1 > _highestUnlockedLevel) _highestUnlockedLevel = levelNumber + 1;
            return record.Apply(score, stars);
        }
    }
}
