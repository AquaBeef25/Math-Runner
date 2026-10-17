using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Save;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        [Tooltip("Inactive template button, copied once per level.")]
        [SerializeField] private LevelButton _template;
        [SerializeField] private Transform _grid;
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            _backButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.MainMenu));
            var save = SaveSystem.Data;
            foreach (var level in LevelCatalog.Load().Levels)
            {
                var button = Instantiate(_template, _grid);
                button.gameObject.SetActive(true);
                button.name = "Level " + level.LevelNumber;
                var chosen = level;
                button.Show(level, save.GetRecord(level.LevelNumber), save.IsUnlocked(level.LevelNumber), () => SceneFlow.PlayLevel(chosen));
            }
        }
    }
}
