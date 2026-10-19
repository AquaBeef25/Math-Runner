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

        // Removed by the shockwave: no hit.
        void Fizzle();
    }
}
