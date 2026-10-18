using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Level
{
    // The look of a world: a sky colour plus parallax layers drawn back to front (far painting, clouds, hills...).
    [CreateAssetMenu(menuName = "Math Runner/Background Theme", fileName = "BackgroundTheme")]
    public class BackgroundTheme : ScriptableObject
    {
        [Serializable]
        public class Layer
        {
            [SerializeField] private Sprite _sprite;
            [Tooltip("Share of the camera's movement copied per axis: 0 = fixed in the world, 1 = fixed on screen.")]
            [SerializeField] private Vector2 _follow = new Vector2(0.9f, 0.9f);
            [Tooltip("Layer position when the camera is at the world origin.")]
            [SerializeField] private Vector2 _anchor;
            [Tooltip("Width of one repeat in world units; 0 = no horizontal wrapping.")]
            [SerializeField, Min(0f)] private float _tileWidth = 16f;
            [Tooltip("Drawn size in world units (Tiled draw mode); 0 height = the sprite's own height.")]
            [SerializeField] private Vector2 _size = new Vector2(64f, 0f);
            [SerializeField] private int _order = -30;

            public Sprite Sprite => _sprite;
            public Vector2 Follow => _follow;
            public Vector2 Anchor => _anchor;
            public float TileWidth => _tileWidth;
            public Vector2 Size => _size;
            public int Order => _order;

            public Layer(Sprite sprite, Vector2 follow, Vector2 anchor, float tileWidth, Vector2 size, int order)
            {
                _sprite = sprite;
                _follow = follow;
                _anchor = anchor;
                _tileWidth = tileWidth;
                _size = size;
                _order = order;
            }
        }

        [SerializeField] private Color _skyColor = new Color(0.82f, 0.93f, 0.98f);
        [SerializeField] private List<Layer> _layers = new List<Layer>();

        public Color SkyColor => _skyColor;
        public IReadOnlyList<Layer> Layers => _layers;

        // Editor setup.
        public void SetLayers(Color sky, List<Layer> layers)
        {
            _skyColor = sky;
            _layers = layers;
        }
    }
}
