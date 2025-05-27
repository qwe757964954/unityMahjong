using UnityEngine;

namespace MahjongGame
{
    public static class MahjongConfig
    {
        // Tile dimensions and spacing
        public static float TileWidth { get; } = 0.035f;
        public static float TileSpacing { get; } = 0.002f;
        public static float StackHeight { get; } = 0.021f;

        // Rack and hand positioning
        public static Vector3[] RackPositions { get; } = {
            new Vector3(0.0175f, -0.057f, 0.429f),  // Down
            new Vector3(0.429f, -0.057f, 0.0175f),  // Left
            new Vector3(0.0175f, -0.057f, -0.429f), // Up
            new Vector3(-0.429f, -0.057f, 0.0175f)  // Right
        };
        public static float HandOffsetY { get; } = -0.05f;
        public static float HandOffsetDistance { get; } = 0.08f;

        // Reveal positioning (for game end)
        public static Vector3 RevealHandPositionDown { get; } = new Vector3(0.0455f, 0.709f, 0.693f);
        public static float RevealHandRotationX { get; } = -38f;

        // Animation settings
        public static float AnimationDuration { get; } = 0.5f;
        public static float DealAnimationDelay { get; } = 0.1f;

        // Game settings
        public static int InitialHandCount { get; } = 13;
        public static int EastExtraCard { get; } = 14;
        public static int DefaultPoolSize { get; } = 136;
    }
}