using MathRunner.Core;
using MathRunner.Enemies;
using MathRunner.Math;
using MathRunner.Player;
using MathRunner.Systems;
using UnityEngine;

namespace MathRunner.Level
{
    // A fork in the path as a question: when the runner reaches the zone the question appears with three answers, one
    // chalked on each route's sign, and the world slows (Focus Zone). The route the runner lands on is the answer —
    // full jump = high ledge (choice 1), short hop = middle ledge (2), no jump = low road (3). Right: that route's chest
    // opens. Wrong: that route's mud slows the runner and costs a heart. There is no "missed": not jumping is a choice.
    // Taps and keys 1–4 are ignored here. Same events as an encounter, so scoring, difficulty and the board just work.
    [RequireComponent(typeof(BoxCollider2D))]
    public class ForkZone : MonoBehaviour
    {
        private enum Phase { Waiting, Focus, Resolved }

        private QuestionDirector _director;
        private float _focusScale = 0.5f;
        private float _lead = 6f;
        private bool _timed;
        private ForkPlan _plan;
        private RouteSign[] _signs = new RouteSign[0];
        private MudPatch[] _mud = new MudPatch[0];
        private TreasureChest[] _chests = new TreasureChest[0];

        private Phase _phase;
        private MathQuestion _question;
        private PlayerController _player;
        private Collider2D _playerCollider;
        private Rigidbody2D _playerBody;
        private PlayerHealth _health;
        private float _elapsedReal;

        public ForkPlan Plan => _plan;
        public MathQuestion Question => _question;
        public bool IsFocusActive { get; private set; }
        public int ChosenLane { get; private set; } = -1;
        public MudPatch Mud(int lane) => _mud[lane];
        public TreasureChest Chest(int lane) => _chests[lane];

        public void Configure(QuestionDirector director, float focusScale, float lead, bool timed, ForkPlan plan,
            RouteSign[] signs, MudPatch[] mud, TreasureChest[] chests)
        {
            _director = director;
            _focusScale = focusScale;
            _lead = Mathf.Max(lead, 0.1f);
            _timed = timed;
            _plan = plan;
            _signs = signs;
            _mud = mud;
            _chests = chests;
        }

        private void OnDisable() => EndFocus();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_phase != Phase.Waiting || !other.TryGetComponent(out PlayerController player)) return;
            _player = player;
            _playerCollider = other;
            _playerBody = player.GetComponent<Rigidbody2D>();
            _health = player.GetComponent<PlayerHealth>();
            Fire();
        }

        private void Fire()
        {
            _phase = Phase.Focus;
            _elapsedReal = 0f;
            _question = _director.NextQuestion(ForkPlan.LaneCount);
            for (int i = 0; i < _signs.Length && i < _question.Choices.Count; i++)
                if (_signs[i] != null) _signs[i].Show(_question.Choices[i]);
            IsFocusActive = true;
            GameEvents.RaiseAnswerModeSet(AnswerMode.Route);
            GameEvents.RaiseFocusStarted(_focusScale);
            GameEvents.RaiseQuestionShown(_question, _timed ? _lead : 0f);
        }

        private void Update()
        {
            if (_phase == Phase.Focus) _elapsedReal += WorldTime.RealDeltaTime;
        }

        private void FixedUpdate()
        {
            if (_phase != Phase.Focus || _playerBody == null) return;
            float x = _playerBody.position.x;
            if (x < _plan.CommitX) return;
            float feet = _playerCollider.bounds.min.y;
            int lane = ForkLogic.LaneFor(feet, _plan.BaseTop, _player.IsGrounded, _playerBody.linearVelocity.y);
            // Still in the air at the commit point (a long hop): wait for the landing, but decide before the mud.
            if (lane < 0 && x < _plan.Lanes[ForkPlan.High].MudLeft - 0.3f) return;
            if (lane < 0) lane = ForkLogic.LaneFor(feet, _plan.BaseTop, true, 0f);
            Commit(lane);
        }

        private void Commit(int lane)
        {
            _phase = Phase.Resolved;
            ChosenLane = lane;
            GameEvents.RaiseAnswerLocked(lane);
            EndFocus();

            bool right = _question.IsCorrect(lane);
            if (right)
            {
                if (lane < _chests.Length && _chests[lane] != null) _chests[lane].Arm();
                if (FollowingFly.Current != null) FollowingFly.Current.Kill();
            }
            else
            {
                // The wrong answer costs its heart right away (jumping over the mud must not dodge it); the mud slows.
                if (_health != null) _health.HitByWrongRoute();
                else GameEvents.RaisePlayerHit(HitCause.Mud);
                if (lane < _mud.Length && _mud[lane] != null) _mud[lane].Arm();
                if (FollowingFly.Current != null) FollowingFly.Current.BiteAndLeave(_health);
            }
            float fraction = _timed ? ForkLogic.RemainingFraction(_elapsedReal, _lead) : 0f;
            GameEvents.RaiseQuestionResolved(new QuestionResult(_question, ForkLogic.Outcome(lane, _question.CorrectIndex), lane,
                _timed ? _lead : 0f, fraction));
        }

        private void EndFocus()
        {
            if (!IsFocusActive) return;
            IsFocusActive = false;
            GameEvents.RaiseFocusEnded();
        }
    }
}
