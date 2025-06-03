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
        private readonly Dictionary<int, List<MahjongTile>> winTiles = new(); // Store hu tiles per player
        private void Start()
        {
            ValidateAnchors(chowPungKongAnchors, "ChowPungKong");
            ValidateAnchors(winAnchors, "Win");

            for (int i = 0; i < 4; i++)
            {
                chowPungKongGroups[i] = new List<ChowPungKongGroup>();
                chowPungKongGroupWidths[i] = new List<float>();
            }
        }

        private void ValidateAnchors(Transform[] anchors, string label)
        {
            for (int i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] == null)
                    Debug.LogWarning($"{label} anchor at index {i} is not assigned.");
            }
        }

        public void PlaceChowPungKong(int playerIndex, int targetPlayerIndex, List<MahjongTile> tiles, MahjongTile targetTile = null)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors)) return;
            if (tiles == null || tiles.Count == 0) return;

            var anchor = chowPungKongAnchors[playerIndex];
            var tileWidth = MahjongConfig.TileWidth;
            var tileHeight = MahjongConfig.TileHeight;
            var spacing = MahjongConfig.TileSpacing;

            int insertIndex = tiles.Count;
            if (targetTile != null)
            {
                int prev = (playerIndex + 3) % 4;
                int opp = (playerIndex + 2) % 4;
                int next = (playerIndex + 1) % 4;

                insertIndex = targetPlayerIndex == prev ? 0 : targetPlayerIndex == opp ? 1 : targetPlayerIndex == next ? 3 : tiles.Count;
            }

            List<MahjongTile> ordered = new List<MahjongTile>(tiles);
            if (targetTile != null)
                ordered.Insert(Mathf.Clamp(insertIndex, 0, ordered.Count), targetTile);

            float groupWidth = ordered.Count * (tileWidth + spacing) + spacing * 2;
            if (targetTile != null) groupWidth += (tileHeight - tileWidth);

            float offset = GetGroupOffset(playerIndex);
            chowPungKongGroupWidths[playerIndex].Add(groupWidth);
            float dz = tileHeight - tileWidth;

            ChowPungKongGroup group = new ChowPungKongGroup();

            for (int i = 0; i < ordered.Count; i++)
            {
                var tile = ordered[i];
                var obj = tile.GameObject;
                obj.transform.SetParent(anchor);
                obj.transform.localScale = Vector3.one;
                LayerUtil.SetLayerRecursively(obj, LayerMask.NameToLayer("Default"));

                bool isTarget = (tile == targetTile);
                bool afterTarget = (targetTile != null && i > insertIndex);

                float dx = offset + i * (tileWidth + spacing);
                if (isTarget) dx += dz / 2f;
                if (afterTarget) dx += dz;

                ApplyTileTransform(obj, isTarget, dx);

                group.Tiles.Add(tile);
                if (isTarget) group.TargetTile = tile;
            }

            chowPungKongGroups[playerIndex].Add(group);
        }
        private void ApplyTileTransform(GameObject obj, bool isTarget, float dx)
        {
            float dz = isTarget ? (MahjongConfig.TileHeight - MahjongConfig.TileWidth) / 2f : 0;
            obj.transform.localRotation = isTarget ? Quaternion.Euler(-180, 90, 0) : Quaternion.Euler(-180, 0, 0);
            obj.transform.localPosition = new Vector3(dx, 0, dz);
        }
        
        private float GetGroupOffset(int playerIndex)
        {
            if (!chowPungKongGroupWidths.TryGetValue(playerIndex, out var widths)) return 0f;
            return widths.Count * MahjongConfig.TileGroupSpacing + widths.Sum();
        }
        public void PlaceConcealedKong(int playerIndex, List<MahjongTile> tiles)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors)) return;
            if (tiles == null || tiles.Count != 4) return;

            float tileWidth = MahjongConfig.TileWidth;
            float spacing = MahjongConfig.TileSpacing;

            var anchor = chowPungKongAnchors[playerIndex];
            float totalWidth = 4 * (tileWidth + spacing) + spacing * 2;
            float offset = GetGroupOffset(playerIndex);
            chowPungKongGroupWidths[playerIndex].Add(totalWidth);

            ChowPungKongGroup group = new ChowPungKongGroup();

            for (int i = 0; i < 4; i++)
            {
                var tile = tiles[i];
                var obj = tile.GameObject;
                obj.transform.SetParent(anchor);
                obj.transform.localScale = Vector3.one;
                LayerUtil.SetLayerRecursively(obj, LayerMask.NameToLayer("Default"));

                float dx = offset + i * (tileWidth + spacing);
                obj.transform.localPosition = new Vector3(dx, 0, 0);
                obj.transform.localRotation = (i == 1 || i == 2) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(-180, 0, 0);

                group.Tiles.Add(tile);
            }

            chowPungKongGroups[playerIndex].Add(group);
        }

        /// <summary>
        /// 补杠（根据碰牌堆中的目标牌，叠放一个补牌）
        /// </summary>
        public void SupplementKong(int playerIndex, int groupIndex, MahjongTile tileToAdd)
        {
            if (!chowPungKongGroups.TryGetValue(playerIndex, out var groups) ||
                groupIndex < 0 || groupIndex >= groups.Count)
            {
                Debug.LogWarning("Invalid SupplementKong request.");
                return;
            }

            var group = groups[groupIndex];
            var baseTile = group.TargetTile ?? group.Tiles[0]; // 优先找目标牌，否则默认首张

            if (baseTile == null)
            {
                Debug.LogWarning("Base tile for SupplementKong is null.");
                return;
            }

            GameObject obj = tileToAdd.GameObject;
            obj.transform.SetParent(baseTile.GameObject.transform.parent);
            obj.transform.localScale = Vector3.one;

            // 坐标逻辑：叠加在 baseTile 上，略微后移
            float tileHeight = MahjongConfig.TileHeight;
            float tileWidth = MahjongConfig.TileWidth;
            float dz = - tileWidth;

            Vector3 basePos = baseTile.GameObject.transform.localPosition;
            Vector3 supplementOffset = new Vector3(0, 0, dz);

            obj.transform.localPosition = basePos + supplementOffset;
            obj.transform.localRotation = Quaternion.Euler(-180, 90, 0); // 保持与目标牌一致
            LayerUtil.SetLayerRecursively(obj, LayerMask.NameToLayer("Default"));

            group.Tiles.Add(tileToAdd);
        }
        public void PlaceWinTiles(int playerIndex, MahjongTile huTile)
        {
            if (!IsValidPlayer(playerIndex, winAnchors)) return;
            if (huTile == null)
            {
                Debug.LogWarning("Hu tile is null.");
                return;
            }

            float tileWidth = MahjongConfig.TileWidth;
            float spacing = MahjongConfig.TileSpacing;
            var anchor = winAnchors[playerIndex];

            // Store the hu tile
            if (!winTiles.ContainsKey(playerIndex))
                winTiles[playerIndex] = new List<MahjongTile>();
            winTiles[playerIndex].Add(huTile);

            // Calculate position based on existing tiles
            int tileCount = anchor.childCount;
            const int maxTilesPerLayer = 5;
            int layer = tileCount / maxTilesPerLayer; // Current row (0-based)
            int positionInLayer = tileCount % maxTilesPerLayer; // Position within the row

            float xPosition = -positionInLayer * (tileWidth + spacing);
            float yOffset = layer * MahjongConfig.TileThickness;

            var obj = huTile.GameObject;
            obj.transform.SetParent(anchor);
            obj.transform.localScale = Vector3.one;
            LayerUtil.SetLayerRecursively(obj, LayerMask.NameToLayer("Default"));

            // Apply transform
            ApplyWinTileTransform(obj, xPosition, yOffset);
        }

        private void ApplyWinTileTransform(GameObject obj, float xPosition, float yOffset)
        {
            float zOffset = 0f;
            obj.transform.localPosition = new Vector3(xPosition, yOffset, zOffset);
            obj.transform.localRotation = Quaternion.Euler(-180, 0, 0);
        }
        
        public void ClearChowPungKong(int playerIndex)
        {
            if (!IsValidPlayer(playerIndex, chowPungKongAnchors)) return;

            foreach (Transform child in chowPungKongAnchors[playerIndex])
                Destroy(child.gameObject);

            chowPungKongGroups[playerIndex].Clear();
            chowPungKongGroupWidths[playerIndex].Clear();
        }

        public void ClearWinArea(int playerIndex)
        {
            if (!IsValidPlayer(playerIndex, winAnchors)) return;

            foreach (Transform child in winAnchors[playerIndex])
                Destroy(child.gameObject);

            if (winTiles.ContainsKey(playerIndex))
                winTiles[playerIndex].Clear();
        }

        public List<MahjongTile> GetWinTiles(int playerIndex)
        {
            return winTiles.ContainsKey(playerIndex) ? winTiles[playerIndex] : new List<MahjongTile>();
        }

        private bool IsValidPlayer(int index, Transform[] anchors)
        {
            return index >= 0 && index < anchors.Length && anchors[index] != null;
        }

        private class ChowPungKongGroup
        {
            public List<MahjongTile> Tiles = new();
            public MahjongTile TargetTile;
        }
    }
}
