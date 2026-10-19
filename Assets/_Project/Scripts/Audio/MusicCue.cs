using UnityEngine;

namespace MathRunner.Audio
{
    // Starts this scene's background tune (menus). Levels pick theirs from LevelConfig.Music instead.
    public class MusicCue : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;

        public AudioClip Clip => _clip;

        // Editor setup.
        public void SetClip(AudioClip clip) => _clip = clip;

        private void Start() => MusicPlayer.Play(_clip);
    }
}
