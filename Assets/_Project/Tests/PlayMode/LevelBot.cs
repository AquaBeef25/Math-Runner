using System;
using System.Collections.Generic;
using System.Linq;
using MathRunner.Core;
using MathRunner.Input;
using MathRunner.Level;
using MathRunner.Math;
using MathRunner.Player;
using UnityEngine;

namespace MathRunner.Tests.PlayMode
{
    public enum AnswerPlan { Correct, Wrong, Miss }

    public enum HazardKind { Spike, Gap }

    public readonly struct Hazard
    {
        public Hazard(HazardKind kind, float x, int timesReached)
        {
            Kind = kind;
            X = x;
            TimesReached = timesReached;
        }

        public HazardKind Kind { get; }
        public float X { get; }
        public int TimesReached { get; } // 1 on the first approach, 2 after a respawn, ...
    }

    // Plays the loaded level through InputReader: picks answers (as if tapping a button) and jumps hazards.
    // Call BeforeFrame(), advance one frame, then AfterFrame().
    public sealed class LevelBot : IDisposable
    {
        private const int GapHoldFrames = 22;
        private const int SpikeHoldFrames = 5;
        private const float SpikeLeadTime = 0.27f;
        private const float GapLeadTime = 0.1f;

        private sealed class HazardState
        {
            public HazardKind Kind;
            public float X;
            public int Reached;
        }

        private readonly PlayerController _player;
        private readonly InputReader _input;
        private readonly List<HazardState> _hazards = new List<HazardState>();
        // On a generated level the bot follows the planner's reference jumps instead of reacting to spikes and gaps.
        private readonly List<(float X, float HoldSeconds)> _planJumps;
        private int _nextJump;
        private bool _pendingJump;
        private float _pendingHold;
        private float _pendingFromX;
        private int _pendingPick = -1;
        private float _sinceShown; // real seconds, so slow-mo doesn't stretch the pick delay
        private int _holdLeft;
        private float _lastX;
        // Forks in the path are answered by jumping onto the answer's route: full jump, short hop or no jump.
        private readonly List<ForkPlan> _forks = new List<ForkPlan>();
        private bool _routeMode;
        private bool _forkJumpPending;
        private float _forkJumpX;
        private float _forkHold;

        public LevelBot()
        {
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            _input = UnityEngine.Object.FindFirstObjectByType<InputReader>();
            _lastX = _player.transform.position.x;

            var builder = UnityEngine.Object.FindFirstObjectByType<LevelBuilder>();
            if (builder != null && builder.Plan != null)
            {
                _planJumps = builder.Plan.Jumps.OrderBy(j => j.X).ToList();
                _forks.AddRange(builder.Plan.Forks);
                while (_nextJump < _planJumps.Count && _planJumps[_nextJump].X < _lastX) _nextJump++;
            }
            else
            {
                FindHazardsInScene();
            }

            GameEvents.AnswerModeSet += OnAnswerModeSet;
            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.QuestionResolved += OnQuestionResolved;
            GameEvents.PlayerHit += OnPlayerHit;
        }

        // Hand-built scenes (Sandbox_Level): react to every spike and gap found in the scene.
        private void FindHazardsInScene()
        {
            foreach (var o in UnityEngine.Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
                _hazards.Add(new HazardState { Kind = HazardKind.Spike, X = o.transform.position.x });

            int ground = LayerMask.NameToLayer("Ground");
            var floors = UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
                .Where(c => c.gameObject.layer == ground && !c.isTrigger)
                .Select(c => c.bounds).OrderBy(b => b.min.x).ToArray();
            for (int i = 0; i + 1 < floors.Length; i++)
            {
                if (floors[i + 1].min.x - floors[i].max.x > 0.5f)
                    _hazards.Add(new HazardState { Kind = HazardKind.Gap, X = floors[i].max.x });
            }
        }

        public Func<int, AnswerPlan> AnswerPlanFor { get; set; } = _ => AnswerPlan.Correct;
        public Func<Hazard, bool> ShouldJump { get; set; } = _ => true;
        // Real seconds to wait after a question appears before picking. 0 = pick straight away (full speed bonus).
        public float PickDelaySeconds { get; set; }
        // Optional: choose the answer index directly (question number, question) → index, or -1 for no pick.
        // Overrides AnswerPlanFor when set.
        public Func<int, MathQuestion, int> PickFor { get; set; }
        public List<QuestionResult> Results { get; } = new List<QuestionResult>();
        public List<HitCause> Hits { get; } = new List<HitCause>();
        public int QuestionsShown { get; private set; }
        public bool Respawned { get; private set; }

        public void BeforeFrame()
        {
            float x = _player.transform.position.x;
            float speed = _player.RunSpeed;
            if (x < _lastX - 1f) Respawned = true;

            _sinceShown += WorldTime.RealDeltaTime;
            if (_pendingPick >= 0 && _sinceShown >= PickDelaySeconds)
            {
                _input.NotifyAnswerChosen(_pendingPick);
                _pendingPick = -1;
            }

            if (_planJumps != null) FollowPlan(x);
            if (_forkJumpPending && x >= _forkJumpX && _holdLeft == 0 && _player.IsGrounded)
            {
                Press(Mathf.Max(1, Mathf.CeilToInt(_forkHold / Mathf.Max(WorldTime.RealDeltaTime, 1e-4f))));
                _forkJumpPending = false;
            }

            foreach (var h in _hazards)
            {
                float trigger = h.X - speed * (h.Kind == HazardKind.Spike ? SpikeLeadTime : GapLeadTime);
                if (!(_lastX < trigger && x >= trigger)) continue;
                h.Reached++;
                bool canPress = _holdLeft == 0 && _player.IsGrounded;
                if (canPress && ShouldJump(new Hazard(h.Kind, h.X, h.Reached)))
                    Press(h.Kind == HazardKind.Spike ? SpikeHoldFrames : GapHoldFrames);
            }

            _lastX = x;
        }

        public void AfterFrame()
        {
            if (_holdLeft > 0 && --_holdLeft == 0) _input.NotifyJumpReleased();
        }

        public void Dispose()
        {
            GameEvents.AnswerModeSet -= OnAnswerModeSet;
            GameEvents.QuestionShown -= OnQuestionShown;
            GameEvents.QuestionResolved -= OnQuestionResolved;
            GameEvents.PlayerHit -= OnPlayerHit;
            if (_input != null) _input.NotifyJumpReleased();
        }

        // Press at each planned take-off; if still airborne there, press on landing unless it's more than a unit late.
        private void FollowPlan(float x)
        {
            if (x < _lastX - 1f)
            {
                _nextJump = 0;
                while (_nextJump < _planJumps.Count && _planJumps[_nextJump].X < x) _nextJump++;
                _pendingJump = false;
            }
            while (_nextJump < _planJumps.Count && x >= _planJumps[_nextJump].X)
            {
                _pendingJump = true;
                _pendingHold = _planJumps[_nextJump].HoldSeconds;
                _pendingFromX = _planJumps[_nextJump].X;
                _nextJump++;
            }
            if (!_pendingJump) return;
            if (x > _pendingFromX + 1f) { _pendingJump = false; return; }
            if (_holdLeft == 0 && _player.IsGrounded)
            {
                // Hold is in real seconds: the player measures held time unslowed, so the arc matches at any speed.
                Press(Mathf.Max(1, Mathf.CeilToInt(_pendingHold / Mathf.Max(WorldTime.RealDeltaTime, 1e-4f))));
                _pendingJump = false;
            }
        }

        private void Press(int holdFrames)
        {
            _input.NotifyJumpPressed();
            _holdLeft = holdFrames;
        }

        private void OnAnswerModeSet(AnswerMode mode) => _routeMode = mode == AnswerMode.Route;

        private void OnQuestionShown(MathQuestion question, float timeLimit)
        {
            int number = QuestionsShown++;
            _sinceShown = 0f;
            int pick;
            if (PickFor != null) pick = PickFor(number, question);
            else
            {
                var plan = AnswerPlanFor(number);
                int count = question.Choices.Count;
                pick = plan == AnswerPlan.Correct ? question.CorrectIndex
                    : plan == AnswerPlan.Wrong ? (question.CorrectIndex + 1) % count
                    : -1;
            }
            if (!_routeMode)
            {
                _pendingPick = pick;
                return;
            }
            // A fork: take the picked route (no pick = the low road, which needs no jump).
            _pendingPick = -1;
            float x = _player.transform.position.x;
            var fork = _forks.FirstOrDefault(f => f.SplitX >= x - 1f && f.SplitX - x < 60f);
            int lane = pick < 0 ? ForkPlan.Low : pick;
            _forkJumpPending = fork != null && lane != ForkPlan.Low;
            if (!_forkJumpPending) return;
            _forkJumpX = fork.Lanes[lane].TakeoffX;
            _forkHold = fork.Lanes[lane].HoldSeconds;
        }

        private void OnQuestionResolved(QuestionResult result) => Results.Add(result);

        private void OnPlayerHit(HitCause cause) => Hits.Add(cause);
    }
}
