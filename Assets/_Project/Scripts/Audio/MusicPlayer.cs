using MathRunner.Core;
using UnityEngine;

namespace MathRunner.Audio
{
    // The one background-music source. It lives across scene loads, so the menu tune keeps playing from the main menu
    // into level select, and changes with a short fade when a level of another world starts. Quieter while paused.
    // Fades run on unscaled time: pause and the Focus slow-motion never stall them.
    public class MusicPlayer : MonoBehaviour
    {
        private const float FadeSeconds = 0.6f;
        private const float Volume = 0.45f;
        private const float PausedVolume = 0.2f;

        private static MusicPlayer _instance;

        private AudioSource _source;
        private AudioClip _next;
        private bool _paused;

        private static MusicPlayer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("Music");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MusicPlayer>();
                return _instance;
            }
        }

        // Plays `clip` looping, fading over from whatever plays now. The same clip keeps playing without a restart.
        public static void Play(AudioClip clip)
        {
            if (clip != null) Instance.Request(clip);
        }

        // One step of a fade: moves `current` toward `target` by dt / seconds (a full fade takes `seconds`).
        public static float Fade(float current, float target, float dt, float seconds) =>
            seconds <= 0f ? target : Mathf.MoveTowards(current, target, dt / seconds);

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;
        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnGameStateChanged(GameState state) => _paused = state == GameState.Paused;

        private void Request(AudioClip clip)
        {
            if (clip == _source.clip)
            {
                _next = null;
                if (!_source.isPlaying) _source.Play();
                return;
            }
            if (_source.clip == null)
            {
                _source.clip = clip;
                _source.Play();
                return;
            }
            _next = clip;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_next != null)
            {
                _source.volume = Fade(_source.volume, 0f, dt, FadeSeconds);
                if (_source.volume > 0f) return;
                _source.clip = _next;
                _next = null;
                _source.Play();
                return;
            }
            _source.volume = Fade(_source.volume, _paused ? PausedVolume : Volume, dt, FadeSeconds);
        }
    }
}
