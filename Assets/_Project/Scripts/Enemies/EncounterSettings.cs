using UnityEngine;

namespace MathRunner.Enemies
{
    // Every tunable of an enemy encounter in one place: timing, slow-mo strength, enemy motion, bullet and shockwave.
    [CreateAssetMenu(menuName = "Math Runner/Encounter Settings", fileName = "EncounterSettings")]
    public class EncounterSettings : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("World speed during the Focus Zone (0.2 = five times slower).")]
        [SerializeField, Range(0.05f, 1f)] private float _focusScale = 0.2f;
        [SerializeField, Min(0f)] private float _flyInSeconds = 1.2f;
        [SerializeField, Min(0f)] private float _windUpSeconds = 0.4f;
        [Tooltip("After a wrong answer the fireball or bee strikes this many seconds later (instead of at the deadline).")]
        [SerializeField, Range(0.1f, 1f)] private float _wrongRushSeconds = 0.4f;
        [Tooltip("Ground kept clear after the shot so the enemy can leave before the next hazard.")]
        [SerializeField, Min(0f)] private float _exitBufferSeconds = 1f;

        [Header("Enemy")]
        [Tooltip("Hover point relative to the player.")]
        [SerializeField] private Vector2 _hoverOffset = new Vector2(7f, 4f);
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.25f;
        [SerializeField, Min(0f)] private float _bobHz = 1.5f;
        [SerializeField, Min(0f)] private float _retreatSpeed = 12f;

        [Header("Bullet & shockwave")]
        [SerializeField] private float _bulletSpinDegreesPerSecond = 360f;
        [SerializeField, Min(0f)] private float _shockwaveRadius = 9f;
        [SerializeField, Min(0.01f)] private float _shockwaveSeconds = 0.45f;

        public float FocusScale => _focusScale;
        public float FlyInSeconds => _flyInSeconds;
        public float WindUpSeconds => _windUpSeconds;
        public float WrongRushSeconds => _wrongRushSeconds;
        public float ExitBufferSeconds => _exitBufferSeconds;
        public float SpanSeconds => _flyInSeconds + _windUpSeconds + _exitBufferSeconds;
        public Vector2 HoverOffset => _hoverOffset;
        public float BobAmplitude => _bobAmplitude;
        public float BobHz => _bobHz;
        public float RetreatSpeed => _retreatSpeed;
        public float BulletSpinDegreesPerSecond => _bulletSpinDegreesPerSecond;
        public float ShockwaveRadius => _shockwaveRadius;
        public float ShockwaveSeconds => _shockwaveSeconds;
    }
}
