using MathRunner.Core;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace MathRunner.Audio
{
    // Sound effects only: listens to GameEvents and the player, never changes gameplay (like FeedbackFX).
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private SoundSet _sounds;
        [Tooltip("Optional: scenes without a runner (menus) leave this empty.")]
        [SerializeField] private PlayerController _player;
        [Tooltip("Optional mixer group (e.g. SFX) for volume control.")]
        [SerializeField] private AudioMixerGroup _output;

        private AudioSource _sfx;
        private AudioSource _pitched; // separate so a pitched streak sound never bends other sounds
        private int _streak;
        private bool _counting;
        private float _remaining;

        public int Streak => _streak;
        public SoundSet Sounds => _sounds;

        // Editor setup: assigning a ScriptableObject to a scene field through SerializedProperty is unreliable here.
        public void SetSounds(SoundSet sounds) => _sounds = sounds;

        private void Awake()
        {
            // Sound must never break the game: without a sound set, stay silent (a disabled component gets no OnEnable).
            if (_sounds == null)
            {
                Debug.LogWarning($"{name}: AudioManager has no SoundSet; sound effects are off.", this);
                enabled = false;
                return;
            }

            _sfx = GetComponent<AudioSource>();
            _pitched = gameObject.AddComponent<AudioSource>();
            foreach (var source in new[] { _sfx, _pitched })
            {
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _output;
            }
        }

        private void Start()
        {
            if (!enabled) return;
            // Answer buttons already sound through AnswerLocked, so they don't also click.
            var answers = FindObjectsByType<AnswerButtons>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!System.Array.Exists(answers, a => a.Owns(button)))
                    button.onClick.AddListener(PlayClick);
        }

        private void OnEnable()
        {
            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.AnswerLocked += OnAnswerLocked;
            GameEvents.QuestionResolved += OnQuestionResolved;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.FocusStarted += OnFocusStarted;
            GameEvents.FocusEnded += OnFocusEnded;
            GameEvents.EnemyDefeated += OnEnemyDefeated;
            if (_player != null)
            {
                _player.Jumped += OnJumped;
                _player.Landed += OnLanded;
            }
        }

        private void OnDisable()
        {
            GameEvents.QuestionShown -= OnQuestionShown;
            GameEvents.AnswerLocked -= OnAnswerLocked;
            GameEvents.QuestionResolved -= OnQuestionResolved;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.FocusStarted -= OnFocusStarted;
            GameEvents.FocusEnded -= OnFocusEnded;
            GameEvents.EnemyDefeated -= OnEnemyDefeated;
            if (_player != null)
            {
                _player.Jumped -= OnJumped;
                _player.Landed -= OnLanded;
            }
        }

        // Real (unslowed) seconds, so ticks match the bullet in slow-mo; still stops while paused.
        private void Update()
        {
            if (!_counting) return;
            float previous = _remaining;
            _remaining -= WorldTime.RealDeltaTime;
            if (AudioMath.CrossedTick(previous, _remaining, _sounds.TickFromSecond)) Play(_sounds.Tick);
            if (_remaining <= 0f) _counting = false;
        }

        public void PlayClick() => Play(_sounds.Click);

        private void OnQuestionShown(MathQuestion question, float timeLimit)
        {
            _counting = timeLimit > 0f;
            _remaining = timeLimit;
        }

        private void OnAnswerLocked(int choiceIndex)
        {
            _counting = false;
            Play(_sounds.AnswerLocked);
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            _counting = false;
            if (result.Outcome == AnswerOutcome.Correct)
            {
                _streak++;
                Play(_sounds.Correct, AudioMath.StreakPitch(_streak, _sounds.StreakPitchStep, _sounds.StreakPitchMaxSteps));
                return;
            }

            _streak = 0;
            Play(_sounds.Wrong);
            if (result.Outcome == AnswerOutcome.Wrong) Play(_sounds.Crumble);
        }

        private void OnPlayerHit(HitCause cause) => Play(_sounds.Hit);

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.LevelComplete) Play(_sounds.LevelComplete);
            else if (state == GameState.GameOver) Play(_sounds.GameOver);
        }

        // The shot and the slow-down start together.
        private void OnFocusStarted(float factor)
        {
            Play(_sounds.Fire);
            Play(_sounds.FocusIn);
        }

        private void OnFocusEnded() => Play(_sounds.FocusOut);

        private void OnEnemyDefeated(Vector3 position)
        {
            Play(_sounds.Shockwave);
            Play(_sounds.EnemyPop);
        }

        private void OnJumped() => Play(_sounds.Jump);

        private void OnLanded(float downwardSpeed)
        {
            if (downwardSpeed >= _sounds.LandMinSpeed) Play(_sounds.Land);
        }

        private void Play(SoundSet.Sound sound, float pitch = 1f)
        {
            if (_sounds == null || sound == null) return;
            var clip = sound.Pick();
            if (clip == null) return;

            var source = Mathf.Approximately(pitch, 1f) ? _sfx : _pitched;
            source.pitch = pitch;
            source.PlayOneShot(clip, sound.Volume);
        }
    }
}
