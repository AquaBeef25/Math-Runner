using System;
using MathRunner.Level;
using MathRunner.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    // One Kenney tile on the level select board: green for Meadow, yellow for Desert, grey while locked, with the
    // level's best stars under its number.
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _face;
        [SerializeField] private TextMeshProUGUI _number;
        [SerializeField] private TextMeshProUGUI _topic;
        [SerializeField] private TextMeshProUGUI _detail;
        [SerializeField] private StarRow _stars;

        public static UiButtonColor ColorFor(bool desert, bool unlocked) =>
            !unlocked ? UiButtonColor.Grey : desert ? UiButtonColor.Yellow : UiButtonColor.Green;

        public void Show(LevelConfig level, LevelRecord record, bool unlocked, Action onClick)
        {
            bool desert = level.Terrain != null && level.Terrain.IsDesert;
            _face.sprite = UiTheme.Load().SquareButton(ColorFor(desert, unlocked));
            _number.text = level.LevelNumber.ToString();
            _topic.text = level.Topic;
            _detail.text = !unlocked ? "Locked" : record == null ? "New!" : $"Best {record.BestScore}";
            bool rated = unlocked && record != null;
            _stars.gameObject.SetActive(rated);
            if (rated) _stars.Show(record.BestStars);
            _button.interactable = unlocked;
            _button.onClick.AddListener(() => onClick());
        }
    }
}
