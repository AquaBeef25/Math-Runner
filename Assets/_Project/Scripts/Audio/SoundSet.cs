using System;
using UnityEngine;

namespace MathRunner.Audio
{
    // Every sound effect the game plays, with variants picked at random so repeats don't sound robotic.
    [CreateAssetMenu(menuName = "Math Runner/Sound Set", fileName = "SoundSet")]
    public class SoundSet : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
            [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

            public float Volume => _volume;
            public int ClipCount => _clips == null ? 0 : _clips.Length;

            public AudioClip Pick() => ClipCount == 0 ? null : _clips[UnityEngine.Random.Range(0, _clips.Length)];
        }

        [Header("Player")]
        [SerializeField] private Sound _jump = new Sound();
        [SerializeField] private Sound _land = new Sound();
        [SerializeField] private Sound _hit = new Sound();

        [Header("Questions")]
        [SerializeField] private Sound _answerLocked = new Sound();
        [SerializeField] private Sound _correct = new Sound();
        [SerializeField] private Sound _wrong = new Sound();
        [SerializeField] private Sound _crumble = new Sound();
        [SerializeField] private Sound _tick = new Sound();

        [Header("Level")]
        [SerializeField] private Sound _levelComplete = new Sound();
        [SerializeField] private Sound _gameOver = new Sound();

        [Header("UI")]
        [SerializeField] private Sound _click = new Sound();

        [Header("Enemy encounter")]
        [SerializeField] private Sound _fire = new Sound();
        [SerializeField] private Sound _shockwave = new Sound();
        [SerializeField] private Sound _enemyPop = new Sound();
        [SerializeField] private Sound _focusIn = new Sound();
        [SerializeField] private Sound _focusOut = new Sound();

        [Header("Tuning")]
        [Tooltip("Pitch added per correct answer in a row.")]
        [SerializeField, Range(0f, 0.2f)] private float _streakPitchStep = 0.06f;
        [SerializeField, Min(0)] private int _streakPitchMaxSteps = 8;
        [Tooltip("Countdown ticks play for the last N seconds of a timed question.")]
        [SerializeField, Min(0)] private int _tickFromSecond = 3;
        [Tooltip("Landings slower than this (units/s downward) stay silent.")]
        [SerializeField, Min(0f)] private float _landMinSpeed = 4f;

        public Sound Jump => _jump;
        public Sound Land => _land;
        public Sound Hit => _hit;
        public Sound AnswerLocked => _answerLocked;
        public Sound Correct => _correct;
        public Sound Wrong => _wrong;
        public Sound Crumble => _crumble;
        public Sound Tick => _tick;
        public Sound LevelComplete => _levelComplete;
        public Sound GameOver => _gameOver;
        public Sound Click => _click;
        public Sound Fire => _fire;
        public Sound Shockwave => _shockwave;
        public Sound EnemyPop => _enemyPop;
        public Sound FocusIn => _focusIn;
        public Sound FocusOut => _focusOut;

        public float StreakPitchStep => _streakPitchStep;
        public int StreakPitchMaxSteps => _streakPitchMaxSteps;
        public int TickFromSecond => _tickFromSecond;
        public float LandMinSpeed => _landMinSpeed;

        // For tests: every sound that should have at least one clip.
        public Sound[] All => new[] { _jump, _land, _hit, _answerLocked, _correct, _wrong, _crumble, _tick, _levelComplete, _gameOver, _click, _fire, _shockwave, _enemyPop, _focusIn, _focusOut };
    }
}
