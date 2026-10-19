using MathRunner.Core;
using MathRunner.Level;
using MathRunner.Player;
using MathRunner.Systems;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MathRunner.Tests
{
    // The pieces a fork needs: reading which route the runner took, 3-choice questions, the mud hit and the speed factor.
    public class ForkLogicTests
    {
        [Test]
        public void LaneFollowsWhereTheFeetLand()
        {
            Assert.That(ForkLogic.LaneFor(2.7f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.High));
            Assert.That(ForkLogic.LaneFor(1f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.Middle));
            Assert.That(ForkLogic.LaneFor(-1f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.Low));
            Assert.That(ForkLogic.LaneFor(1.84f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.Middle), "just below the half-way band");
            Assert.That(ForkLogic.LaneFor(1.86f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.High));
            Assert.That(ForkLogic.LaneFor(0.49f + 3f, 3f, true, 0f), Is.EqualTo(ForkPlan.Low));
        }

        [Test]
        public void NoLaneWhileInTheAirOrRising()
        {
            Assert.That(ForkLogic.LaneFor(4f, 3f, false, -2f), Is.EqualTo(-1), "airborne");
            Assert.That(ForkLogic.LaneFor(4f, 3f, true, 3f), Is.EqualTo(-1), "rising through a one-way ledge");
        }

        [Test]
        public void OutcomeAndRemainingTime()
        {
            Assert.That(ForkLogic.Outcome(1, 1), Is.EqualTo(AnswerOutcome.Correct));
            Assert.That(ForkLogic.Outcome(0, 2), Is.EqualTo(AnswerOutcome.Wrong));
            Assert.That(ForkLogic.RemainingFraction(1f, 4f), Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(ForkLogic.RemainingFraction(5f, 4f), Is.EqualTo(0f));
            Assert.That(ForkLogic.RemainingFraction(1f, 0f), Is.EqualTo(0f), "untimed: no speed bonus");
        }

        [Test]
        public void DirectorGivesThreeChoicesForAFork()
        {
            var go = new GameObject("Director");
            var director = go.AddComponent<QuestionDirector>();
            director.Configure(AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/ScriptableObjects/Levels/Level03.asset"));
            for (int i = 0; i < 20; i++)
            {
                var q = director.NextQuestion(3);
                Assert.That(q.Choices.Count, Is.EqualTo(3));
                Assert.That(q.CorrectIndex, Is.InRange(0, 2));
            }
            Assert.That(director.NextQuestion().Choices.Count, Is.EqualTo(4));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void WrongRouteCostsAHeartAsMud()
        {
            var go = new GameObject("Runner", typeof(Rigidbody2D), typeof(BoxCollider2D));
            var health = go.AddComponent<PlayerHealth>();
            HitCause? seen = null;
            void OnHit(HitCause c) => seen = c;
            GameEvents.PlayerHit += OnHit;
            try
            {
                health.HitByWrongRoute();
            }
            finally
            {
                GameEvents.PlayerHit -= OnHit;
                Object.DestroyImmediate(go);
            }
            Assert.That(seen, Is.EqualTo(HitCause.Mud));
        }

        [Test]
        public void SpeedFactorStaysInRange()
        {
            var go = new GameObject("Runner", typeof(Rigidbody2D), typeof(BoxCollider2D));
            var controller = go.AddComponent<PlayerController>();
            Assert.That(controller.SpeedFactor, Is.EqualTo(1f));
            controller.SpeedFactor = 0.5f;
            Assert.That(controller.SpeedFactor, Is.EqualTo(0.5f));
            controller.SpeedFactor = 0f;
            Assert.That(controller.SpeedFactor, Is.EqualTo(0.2f));
            controller.SpeedFactor = 3f;
            Assert.That(controller.SpeedFactor, Is.EqualTo(1f));
            Object.DestroyImmediate(go);
        }
    }
}
