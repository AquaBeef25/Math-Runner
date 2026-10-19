namespace MathRunner.Level
{
    // Who an encounter meets (picked per encounter by the planner). Append only: level assets store weights by index.
    // Frog is retired (2026-10-19): the planner never picks it. The value stays so the others keep their numbers.
    public enum EnemyKind { Alien, Bee, Frog, Fly }
}
