using System;
using UnityEngine;

namespace MathRunner.Enemies
{
    // Whatever reaches the runner when an encounter's time runs out: the alien's fireball, or the bee or fly itself.
    public interface IEncounterAttack
    {
        // Fires once when the attack reaches its target.
        event Action Arrived;

        float RemainingFraction { get; }

        void Launch(Transform target, float flightSeconds);

        // A wrong answer: strike within `seconds` instead of at the deadline (never later than already planned).
        void Rush(float seconds);

        // Removed by the shockwave: no hit.
        void Fizzle();
    }
}
