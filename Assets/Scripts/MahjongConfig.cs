using UnityEngine;

namespace MahjongGame
{
    public static class MahjongConfig
    {
        // Tile dimensions and spacing
        public static float TileWidth { get; } = 0.04f;
        public static float TileHeight { get; } = 0.0531f;
        
        public static float TileSpacing { get; } = 0.000f;
        public static float StackHeight { get; } = 0.0273f;
        
        // Animation settings
        public static float AnimationDuration { get; } = 0.5f;
        public static float DealAnimationDelay { get; } = 0.1f;

        // Game settings
        public static int InitialHandCount { get; } = 13;
        public static int EastExtraCard { get; } = 14;
        public static int DefaultPoolSize { get; } = 136;
        public static readonly Vector3 PerspectiveTileScale = new Vector3(1f, 1f, 1f); // 实际按需调整

    }
}