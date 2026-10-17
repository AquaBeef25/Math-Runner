using System;
using MathRunner.Level;
using MathRunner.Save;
using MathRunner.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _number;
        [SerializeField] private TextMeshProUGUI _topic;
        [SerializeField] private TextMeshProUGUI _detail;

        public void Show(LevelConfig level, LevelRecord record, bool unlocked, Action onClick)
        {
            _number.text = level.LevelNumber.ToString();
            _topic.text = level.Topic;
            _detail.text = !unlocked ? "Locked" : record == null ? "New!" : $"{StarRating.Display(record.BestStars)}\nBest {record.BestScore}";
            _button.interactable = unlocked;
            _button.onClick.AddListener(() => onClick());
        }
    }
}
