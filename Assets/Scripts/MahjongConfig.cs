// ========== Configuration Class ==========
using UnityEngine;

namespace MahjongGame
{
    public static class MahjongConfig
    {
        // Tile dimensions and spacing
        public static float TileWidth = 0.035f;
        public static float TileSpacing = 0.002f;
        public static float StackHeight = 0.021f;
        
        // Animation settings
        public static float AnimationDuration = 0.5f;
        public static float DealAnimationDelay = 0.1f;
        
        // Game settings
        public static int InitialHandCount = 13;
        public static int EastExtraCard = 14;
        public static float HandOffsetY = -0.05f;
        public static float HandOffsetDistance = 0.08f;
        
        // Pool settings
        public static int DefaultPoolSize = 136;
    }
}