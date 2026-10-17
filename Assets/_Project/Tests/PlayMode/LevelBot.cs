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

    // Plays the loaded level through InputReader. Call BeforeFrame(), advance one frame, then AfterFrame().
    public sealed class LevelBot : IDisposable
    {
        private const int PlatformHoldFrames = 22;
        private const int GapHoldFrames = 22;
        private const int SpikeHoldFrames = 5;
        // Seconds from take-off to landing on a 1.4u answer platform with a held jump; distance = run speed × this.
        private const float AirTimeToPlatform = 0.65f;
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
        private readonly QuestionZone[] _zones;
        private readonly List<HazardState> _hazards = new List<HazardState>();
        private int _pendingTarget = -1;
        private int _holdLeft;
        private float _lastX;

        public LevelBot()
        {
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            _input = UnityEngine.Object.FindFirstObjectByType<InputReader>();
            _zones = UnityEngine.Object.FindObjectsByType<QuestionZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.position.x).ToArray();
            _lastX = _player.transform.position.x;

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

            GameEvents.QuestionShown += OnQuestionShown;
            GameEvents.QuestionResolved += OnQuestionResolved;
            GameEvents.PlayerHit += OnPlayerHit;
        }

        public Func<int, AnswerPlan> AnswerPlanFor { get; set; } = _ => AnswerPlan.Correct;
        public Func<Hazard, bool> ShouldJump { get; set; } = _ => true;
        public List<QuestionResult> Results { get; } = new List<QuestionResult>();
        public List<HitCause> Hits { get; } = new List<HitCause>();
        public int QuestionsShown { get; private set; }
        public bool Respawned { get; private set; }

        public void BeforeFrame()
        {
            float x = _player.transform.position.x;
            float speed = _player.RunSpeed;
            if (x < _lastX - 1f) Respawned = true;

            if (_holdLeft == 0 && _pendingTarget >= 0)
            {
                var zone = _zones.LastOrDefault(z => z.transform.position.x <= x + 2f);
                if (zone != null)
                {
                    var platforms = zone.GetComponentsInChildren<AnswerPlatform>().OrderBy(p => p.transform.position.x).ToArray();
                    if (x >= platforms[_pendingTarget].transform.position.x - speed * AirTimeToPlatform)
                    {
                        Press(PlatformHoldFrames);
                        _pendingTarget = -1;
                    }
                }
            }

            foreach (var h in _hazards)
            {
                float trigger = h.X - speed * (h.Kind == HazardKind.Spike ? SpikeLeadTime : GapLeadTime);
                if (!(_lastX < trigger && x >= trigger)) continue;
                h.Reached++;
                bool canPress = _holdLeft == 0 && _pendingTarget < 0 && _player.IsGrounded;
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
            GameEvents.QuestionShown -= OnQuestionShown;
            GameEvents.QuestionResolved -= OnQuestionResolved;
            GameEvents.PlayerHit -= OnPlayerHit;
            if (_input != null) _input.NotifyJumpReleased();
        }

        private void Press(int holdFrames)
        {
            _input.NotifyJumpPressed();
            _holdLeft = holdFrames;
        }

        private void OnQuestionShown(MathQuestion question, float timeLimit)
        {
            var plan = AnswerPlanFor(QuestionsShown);
            QuestionsShown++;
            int count = question.Choices.Count;
            _pendingTarget = plan == AnswerPlan.Correct ? question.CorrectIndex
                : plan == AnswerPlan.Wrong ? (question.CorrectIndex + 1) % count
                : -1;
        }

        private void OnQuestionResolved(QuestionResult result) => Results.Add(result);

        private void OnPlayerHit(HitCause cause) => Hits.Add(cause);
    }
}
