using MathRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            _playButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.LevelSelect));
            _quitButton.onClick.AddListener(Application.Quit);
        }
    }
}
