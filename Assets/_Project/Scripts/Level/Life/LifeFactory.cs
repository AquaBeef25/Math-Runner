using UnityEngine;

namespace MathRunner.Level
{
    // Turns plan entries into harmless moving life: plain GameObjects with SpriteRenderers and a motion component, on the
    // Default layer and never with a Collider2D, so nothing here can touch the runner.
    public static class LifeFactory
    {
        private const int GroundOrder = 4, FlyerOrder = 7, PropOrder = 3, SwingOrder = -4, FishOrder = 2, EmoteOrder = 12;
        // A swinging crate hangs well above the runner's head (never reads as an obstacle) from a chain that starts above
        // the top of the screen (the camera shows about 6 units above and below the runner).
        private const float SwingChain = 7.5f, CrateHeight = 4f;
        private static readonly Color SwingTint = new Color(0.9f, 0.95f, 1f, 0.85f);

        public static GameObject Build(LifeKind kind, int variant, float x, float y, LifeSet set, Transform player, Transform parent)
        {
            var frames = set.Frames(kind, variant);
            var root = new GameObject("Life_" + kind);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, y, 0f);
            float phase = Mathf.Repeat(x * 0.37f, 10f); // neighbours don't move in step

            switch (kind)
            {
                case LifeKind.Walker:
                {
                    var critter = root.AddComponent<Critter>();
                    critter.SetBody(Renderer(root.transform, GroundOrder), frames, 5f);
                    critter.Configure(x, x + LevelPlanner.LifePatrol, 0.6f);
                    AddEmote(root.transform, set, variant + Mathf.FloorToInt(x), player);
                    break;
                }
                case LifeKind.Hopper:
                {
                    var hopper = root.AddComponent<Hopper>();
                    hopper.SetBody(Renderer(root.transform, GroundOrder), frames, 1f);
                    hopper.Configure(y, phase);
                    AddEmote(root.transform, set, variant + Mathf.FloorToInt(x), player);
                    break;
                }
                case LifeKind.Flyer:
                {
                    var flyer = root.AddComponent<Flyer>();
                    flyer.SetBody(Renderer(root.transform, FlyerOrder), frames, 10f);
                    flyer.Configure(root.transform.position, phase);
                    break;
                }
                case LifeKind.Flag:
                case LifeKind.Torch:
                {
                    var sr = Renderer(root.transform, PropOrder);
                    var flipbook = sr.gameObject.AddComponent<SpriteFlipbook>();
                    flipbook.SetFrames(frames, kind == LifeKind.Torch ? 8f : 4f);
                    if (frames.Length > 0) sr.sprite = frames[0];
                    break;
                }
                case LifeKind.Swing:
                {
                    // Pivot above the screen; chain down to the crate; the pivot swings.
                    var pivot = new GameObject("Pivot").transform;
                    pivot.SetParent(root.transform, false);
                    pivot.localPosition = new Vector3(0f, CrateHeight + SwingChain + 0.5f, 0f);
                    pivot.gameObject.AddComponent<Swinger>().Configure(phase);
                    if (frames.Length > 0)
                    {
                        var chain = Renderer(pivot, SwingOrder);
                        chain.name = "Chain";
                        chain.sprite = frames[0];
                        chain.drawMode = SpriteDrawMode.Tiled;
                        chain.size = new Vector2(0.5f, SwingChain);
                        chain.transform.localPosition = new Vector3(0f, -SwingChain * 0.5f, 0f);
                        chain.color = SwingTint;
                    }
                    if (frames.Length > 1)
                    {
                        var crate = Renderer(pivot, SwingOrder);
                        crate.name = "Crate";
                        crate.sprite = frames[1];
                        crate.transform.localPosition = new Vector3(0f, -SwingChain - 0.5f, 0f);
                        crate.color = SwingTint;
                    }
                    break;
                }
            }
            return root;
        }

        public static GameObject BuildFish(float left, float right, float surface, LifeSet set, Transform parent)
        {
            var root = new GameObject("Life_Fish");
            root.transform.SetParent(parent, false);
            var center = new Vector3((left + right) * 0.5f, surface, 0f);
            root.transform.position = center;
            int variant = Mathf.Abs(Mathf.FloorToInt(left)) % Mathf.Max(1, set.FishVariants);
            var fish = root.AddComponent<FishJumper>();
            fish.SetBody(Renderer(root.transform, FishOrder), set.FishFrames(variant), 6f);
            fish.Configure(center, Mathf.Clamp(right - left - 0.5f, 0.5f, 2f), Mathf.Repeat(left * 0.53f, 3.5f));
            return root;
        }

        private static SpriteRenderer Renderer(Transform parent, int order)
        {
            var go = new GameObject("Sprite");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        private static void AddEmote(Transform owner, LifeSet set, int pick, Transform player)
        {
            var emotes = set.Emotes;
            if (emotes == null || emotes.Length == 0 || player == null) return;
            var bubble = Renderer(owner, EmoteOrder);
            bubble.name = "Emote";
            bubble.sprite = emotes[Mathf.Abs(pick) % emotes.Length];
            bubble.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            owner.gameObject.AddComponent<EmoteBubble>().Configure(player, bubble);
        }
    }
}
