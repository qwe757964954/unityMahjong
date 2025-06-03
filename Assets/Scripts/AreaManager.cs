using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MahjongGame
{
    public class AreaManager : MonoBehaviour
    {
        [Header("Chow Pung Kong Anchors (Down, Left, Up, Right)")]
        [SerializeField] private Transform[] chowPungKongAnchors = new Transform[4];

        [Header("Win Area Anchors (Down, Left, Up, Right)")]
        [SerializeField] private Transform[] winAnchors = new Transform[4];

        private readonly Dictionary<int, List<ChowPungKongGroup>> chowPungKongGroups = new();
        private readonly Dictionary<int, List<float>> chowPungKongGroupWidths = new();
        private readonly Dictionary<int, List<MahjongTile>> winTiles = new();

        // Initialize dictionaries and validate anchors on start
        private void Start()
        {
            ValidateAnchors(chowPungKongAnchors, "ChowPungKong");
            ValidateAnchors(winAnchors, "Win");

            for (int i = 0; i < 4; i++)
            {
                chowPungKongGroups[i] = new List<ChowPungKongGroup>();
                chowPungKongGroupWidths[i] = new List<float>();
                winTiles[i] = new List<MahjongTile>();
            }
        }

        // Validate that all anchors are assigned
        private void ValidateAnchors(Transform[] anchors, string label)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] == null)
                {
                    Debug.LogWarning($"{label} anchor at index {i} is not assigned.");
                }
            }
        }

        // Place Chow, Pung, or Kong tiles for a player
        public void PlaceChowPungKong(int playerIndex, int targetPlayerIndex, List<MahjongTile> tiles, MahjongTile targetTile = null)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors) || tiles == null || tiles.Count == 0)
            {
                return;
            }

            var anchor = chowPungKongAnchors[playerIndex];
            var tileWidth = MahjongConfig.TileWidth;
            var tileHeight = MahjongConfig.TileHeight;
            var spacing = MahjongConfig.TileSpacing;

            // Determine insertion index based on target player
            int insertIndex = DetermineInsertIndex(playerIndex, targetPlayerIndex, tiles.Count, targetTile);

            // Prepare ordered tile list
            List<MahjongTile> ordered = new List<MahjongTile>(tiles);
            if (targetTile != null)
            {
                ordered.Insert(Mathf.Clamp(insertIndex, 0, ordered.Count), targetTile);
            }

            // Calculate group width
            float groupWidth = CalculateGroupWidth(ordered.Count, tileWidth, tileHeight, spacing, targetTile);
            float offset = GetGroupOffset(playerIndex);
            chowPungKongGroupWidths[playerIndex].Add(groupWidth);

            // Create and populate new group
            var group = new ChowPungKongGroup();
            float dz = tileHeight - tileWidth;

            for (int i = 0; i < ordered.Count; i++)
            {
                var tile = ordered[i];
                var obj = tile.GameObject;
                ConfigureTileObject(obj, anchor);

                bool isTarget = (tile == targetTile);
                bool afterTarget = (targetTile != null && i > insertIndex);
                float dx = offset + i * (tileWidth + spacing) + (isTarget ? dz / 2f : 0) + (afterTarget ? dz : 0);

                ApplyTileTransform(obj, isTarget, dx);
                group.Tiles.Add(tile);
                if (isTarget) group.TargetTile = tile;
            }

            chowPungKongGroups[playerIndex].Add(group);
        }

        // Place a concealed Kong (four tiles)
        public void PlaceConcealedKong(int playerIndex, List<MahjongTile> tiles)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors) || tiles == null || tiles.Count != 4)
            {
                return;
            }

            var anchor = chowPungKongAnchors[playerIndex];
            float tileWidth = MahjongConfig.TileWidth;
            float spacing = MahjongConfig.TileSpacing;
            float totalWidth = 4 * (tileWidth + spacing) + spacing * 2;
            float offset = GetGroupOffset(playerIndex);
            chowPungKongGroupWidths[playerIndex].Add(totalWidth);

            var group = new ChowPungKongGroup();

            for (int i = 0; i < 4; i++)
            {
                var tile = tiles[i];
                var obj = tile.GameObject;
                ConfigureTileObject(obj, anchor);

                float dx = offset + i * (tileWidth + spacing);
                obj.transform.localPosition = new Vector3(dx, 0, 0);
                obj.transform.localRotation = (i == 1 || i == 2) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(-180, 0, 0);

                group.Tiles.Add(tile);
            }

            chowPungKongGroups[playerIndex].Add(group);
        }

        // Supplement an existing Pung to form a Kong
        public void SupplementKong(int playerIndex, int groupIndex, MahjongTile tileToAdd)
        {
            if (!chowPungKongGroups.TryGetValue(playerIndex, out var groups) ||
                groupIndex < 0 || groupIndex >= groups.Count)
            {
                Debug.LogWarning("Invalid SupplementKong request.");
                return;
            }

            var group = groups[groupIndex];
            var baseTile = group.TargetTile ?? group.Tiles[0];
            if (baseTile == null)
            {
                Debug.LogWarning("Base tile for SupplementKong is null.");
                return;
            }

            var obj = tileToAdd.GameObject;
            ConfigureTileObject(obj, baseTile.GameObject.transform.parent);

            float tileWidth = MahjongConfig.TileWidth;
            Vector3 basePos = baseTile.GameObject.transform.localPosition;
            obj.transform.localPosition = basePos + new Vector3(0, 0, -tileWidth);
            obj.transform.localRotation = Quaternion.Euler(-180, 90, 0);

            group.Tiles.Add(tileToAdd);
        }

        // Place winning (Hu) tiles for a player
        public void PlaceWinTiles(int playerIndex, MahjongTile huTile)
        {
            if (!IsValidPlayer(playerIndex, winAnchors) || huTile == null)
            {
                Debug.LogWarning("Hu tile is null or invalid player index.");
                return;
            }

            var anchor = winAnchors[playerIndex];
            float tileWidth = MahjongConfig.TileWidth;
            float spacing = MahjongConfig.TileSpacing;
            float tileThickness = MahjongConfig.TileThickness;
            float tileHeight = MahjongConfig.TileHeight;

            winTiles[playerIndex].Add(huTile);
            int tileCount = anchor.childCount;

            float xPosition, yOffset, zOffset;
            CalculateWinTilePosition(playerIndex, tileCount, tileWidth, spacing, tileThickness, tileHeight, out xPosition, out yOffset, out zOffset);

            var obj = huTile.GameObject;
            ConfigureTileObject(obj, anchor);
            ApplyWinTileTransform(obj, xPosition, yOffset, zOffset);
        }

        // Clear Chow, Pung, Kong tiles for a player
        public void ClearChowPungKong(int playerIndex)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors))
            {
                return;
            }

            foreach (Transform child in chowPungKongAnchors[playerIndex])
            {
                Destroy(child.gameObject);
            }

            chowPungKongGroups[playerIndex].Clear();
            chowPungKongGroupWidths[playerIndex].Clear();
        }

        // Clear winning tiles for a player
        public void ClearWinArea(int playerIndex)
        {
            if (!IsValidPlayer(playerIndex, winAnchors))
            {
                return;
            }

            foreach (Transform child in winAnchors[playerIndex])
            {
                Destroy(child.gameObject);
            }

            winTiles[playerIndex].Clear();
        }

        // Get the list of winning tiles for a player
        public List<MahjongTile> GetWinTiles(int playerIndex)
        {
            return winTiles.TryGetValue(playerIndex, out var tiles) ? tiles : new List<MahjongTile>();
        }

        // Helper: Determine insertion index for Chow/Pung/Kong tiles
        private int DetermineInsertIndex(int playerIndex, int targetPlayerIndex, int tileCount, MahjongTile targetTile)
        {
            if (targetTile == null)
            {
                return tileCount;
            }

            int prev = (playerIndex + 3) % 4;
            int opp = (playerIndex + 2) % 4;
            int next = (playerIndex + 1) % 4;

            return targetPlayerIndex == prev ? 0 :
                   targetPlayerIndex == opp ? 1 :
                   targetPlayerIndex == next ? 3 : tileCount;
        }

        // Helper: Calculate group width for Chow/Pung/Kong
        private float CalculateGroupWidth(int tileCount, float tileWidth, float tileHeight, float spacing, MahjongTile targetTile)
        {
            float groupWidth = tileCount * (tileWidth + spacing) + spacing * 2;
            if (targetTile != null)
            {
                groupWidth += (tileHeight - tileWidth);
            }
            return groupWidth;
        }

        // Helper: Configure tile GameObject properties
        private void ConfigureTileObject(GameObject obj, Transform parent)
        {
            obj.transform.SetParent(parent);
            obj.transform.localScale = Vector3.one;
            LayerUtil.SetLayerRecursively(obj, LayerMask.NameToLayer("Default"));
        }

        // Helper: Apply transform to a tile
        private void ApplyTileTransform(GameObject obj, bool isTarget, float dx)
        {
            float dz = isTarget ? (MahjongConfig.TileHeight - MahjongConfig.TileWidth) / 2f : 0;
            obj.transform.localRotation = isTarget ? Quaternion.Euler(-180, 90, 0) : Quaternion.Euler(-180, 0, 0);
            obj.transform.localPosition = new Vector3(dx, 0, dz);
        }

        // Helper: Calculate position for winning tiles
        private void CalculateWinTilePosition(int playerIndex, int tileCount, float tileWidth, float spacing, float tileThickness, float tileHeight, out float xPosition, out float yOffset, out float zOffset)
        {
            if (playerIndex == 1)
            {
                const int maxTilesPerZGroup = 3;
                int layer = tileCount / maxTilesPerZGroup;
                int positionInZGroup = tileCount % maxTilesPerZGroup;
                xPosition = 0f;
                yOffset = layer * tileThickness;
                zOffset = positionInZGroup * tileHeight;
            }
            else
            {
                int maxTilesPerLayer = playerIndex switch
                {
                    0 => 4,
                    1 => 3,
                    2 or 3 => 5,
                    _ => 5
                };

                int layer = tileCount / maxTilesPerLayer;
                int positionInLayer = tileCount % maxTilesPerLayer;
                xPosition = -positionInLayer * (tileWidth + spacing);
                yOffset = layer * tileThickness;
                zOffset = 0f;
            }
        }

        // Helper: Apply transform to a winning tile
        private void ApplyWinTileTransform(GameObject obj, float xPosition, float yOffset, float zOffset)
        {
            obj.transform.localPosition = new Vector3(xPosition, yOffset, zOffset);
            obj.transform.localRotation = Quaternion.Euler(-180, 0, 0);
        }

        // Helper: Calculate offset for new group
        private float GetGroupOffset(int playerIndex)
        {
            return chowPungKongGroupWidths.TryGetValue(playerIndex, out var widths)
                ? widths.Count * MahjongConfig.TileGroupSpacing + widths.Sum()
                : 0f;
        }

        // Helper: Validate player index and anchor
        private bool IsValidPlayer(int index, Transform[] anchors)
        {
            return index >= 0 && index < anchors.Length && anchors[index] != null;
        }

        // Inner class to represent a group of Chow, Pung, or Kong tiles
        private class ChowPungKongGroup
        {
            public List<MahjongTile> Tiles = new();
            public MahjongTile TargetTile;
        }
    }
}