using System.Collections;
using MathRunner.Core;
using MathRunner.Player;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UIImage = UnityEngine.UI.Image;

namespace MathRunner.UI
{
    // Game feel only: listens to GameEvents and the player, never changes gameplay.
    // Correct → confetti, "+N" popup, small camera bump. Wrong / missed / hit → screen shake and a red flash.
    public class FeedbackFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private CinemachineImpulseSource _impulse;
        [SerializeField] private UIImage _flash;
        [SerializeField] private TextMeshProUGUI _popupTemplate;
        [SerializeField] private ParticleSystem _confetti;
        [SerializeField] private ParticleSystem _dust;

        [Header("Shake")]
        [SerializeField] private float _correctShake = 0.25f;
        [SerializeField] private float _wrongShake = 1f;
        [SerializeField] private float _hitShake = 1.2f;

        [Header("Flash")]
        [SerializeField] private float _flashAlpha = 0.35f;
        [SerializeField] private float _flashSeconds = 0.3f;

        [Header("Popup")]
        [SerializeField] private float _popupRise = 70f;
        [SerializeField] private float _popupSeconds = 0.9f;

        [Header("Particles")]
        [SerializeField] private int _confettiCount = 40;
        [SerializeField] private int _dustCount = 8;
        [Tooltip("Landings slower than this (units/s downward) make no dust, so walking off tiny steps stays quiet.")]
        [SerializeField] private float _dustMinSpeed = 4f;

        private Coroutine _flashRoutine;
        private Collider2D _playerCollider;

        public int ActivePopups { get; private set; }
        public float FlashAlpha => _flash.color.a;

        private void Awake()
        {
            _playerCollider = _player.GetComponent<Collider2D>();
            _popupTemplate.gameObject.SetActive(false);
            SetFlash(0f);
        }

        private void OnEnable()
        {
            GameEvents.QuestionResolved += OnQuestionResolved;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.PlayerHit += OnPlayerHit;
            _player.Landed += OnLanded;
        }

        private void OnDisable()
        {
            GameEvents.QuestionResolved -= OnQuestionResolved;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.PlayerHit -= OnPlayerHit;
            _player.Landed -= OnLanded;
        }

        private void OnQuestionResolved(QuestionResult result)
        {
            if (result.Outcome == AnswerOutcome.Correct)
            {
                Shake(_correctShake);
                Burst(_confetti, _player.transform.position + Vector3.up, _confettiCount);
            }
            else
            {
                Shake(_wrongShake);
                Flash();
            }
        }

        private void OnPlayerHit(HitCause cause)
        {
            Shake(_hitShake);
            Flash();
        }

        private void OnScoreChanged(ScoreUpdate update)
        {
            if (update.Delta > 0) StartCoroutine(Popup("+" + update.Delta));
        }

        private void OnLanded(float downwardSpeed)
        {
            if (downwardSpeed < _dustMinSpeed) return;
            Bounds feet = _playerCollider.bounds;
            Burst(_dust, new Vector3(feet.center.x, feet.min.y, 0f), _dustCount);
        }

        private void Shake(float force)
        {
            if (_impulse != null && force > 0f) _impulse.GenerateImpulseWithForce(force);
        }

        private static void Burst(ParticleSystem system, Vector3 position, int count)
        {
            if (system == null) return;
            system.transform.position = position;
            system.Emit(count);
        }

        private void Flash()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(Tween.Run(_flashSeconds, Ease.Pulse, k => SetFlash(k * _flashAlpha), () => _flashRoutine = null));
        }

        private void SetFlash(float alpha)
        {
            Color c = _flash.color;
            c.a = alpha;
            _flash.color = c;
            _flash.enabled = alpha > 0f;
        }

        private IEnumerator Popup(string text)
        {
            ActivePopups++;
            var popup = Instantiate(_popupTemplate, _popupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = text;
            var rect = popup.rectTransform;
            Vector2 start = rect.anchoredPosition;
            Color color = popup.color;
            yield return Tween.Run(_popupSeconds, Ease.Linear, t =>
            {
                rect.anchoredPosition = start + Vector2.up * (_popupRise * Ease.OutQuad(t));
                rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Ease.OutBack(Mathf.Min(1f, t * 4f)));
                color.a = 1f - Ease.InQuad(t);
                popup.color = color;
            });
            Destroy(popup.gameObject);
            ActivePopups--;
        }
    }
}
