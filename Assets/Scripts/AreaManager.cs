using System.Collections.Generic;
using UnityEngine;

namespace MahjongGame
{
    public class AreaManager : MonoBehaviour
    {
        [Header("Chow Pung Kong (Down, Left, Up, Right)")] [SerializeField]
        public Transform[] chowPungKongTransforms = new Transform[4];

        [Header("Win (Down, Left, Up, Right)")] [SerializeField]
        public Transform[] winTransforms = new Transform[4];

        private Dictionary<int, int> chowPungKongGroupCounts = new Dictionary<int, int>();

        void Start()
        {
            ValidateTransforms(chowPungKongTransforms, "ChowPungKong");
            ValidateTransforms(winTransforms, "Win");

            for (int i = 0; i < 4; i++)
            {
                chowPungKongGroupCounts[i] = 0;
            }
        }

        private void ValidateTransforms(Transform[] transforms, string type)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null)
                {
                    Debug.LogWarning($"{type} transform for player {i} is null.");
                }
            }
        }

        /// <summary>
        /// 吃/碰/明杠
        /// </summary>
        public void PlaceChowPungKong(int operatingPlayerIndex, int targetPlayerIndex, List<MahjongTile> tiles, MahjongTile targetTile = null)
        {
            if (operatingPlayerIndex < 0 || operatingPlayerIndex >= chowPungKongTransforms.Length || chowPungKongTransforms[operatingPlayerIndex] == null)
            {
                Debug.LogWarning($"Invalid operating player index {operatingPlayerIndex} or null transform for ChowPungKong.");
                return;
            }

            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogWarning("Tiles list is null or empty.");
                return;
            }

            Transform anchor = chowPungKongTransforms[operatingPlayerIndex];
            float tileWidth = MahjongConfig.TileWidth;
            float tileHeight = MahjongConfig.TileHeight;
            float tileSpacing = MahjongConfig.TileSpacing;

            if (!chowPungKongGroupCounts.ContainsKey(operatingPlayerIndex))
                chowPungKongGroupCounts[operatingPlayerIndex] = 0;

            int groupCount = chowPungKongGroupCounts[operatingPlayerIndex];
            int tileCountInGroup = tiles.Count + (targetTile != null ? 1 : 0);
            float groupWidth = tileCountInGroup * (tileWidth + tileSpacing);
            float extraWidth = (targetTile != null) ? (tileHeight - tileWidth) : 0f;
            float groupOffset = groupCount * (groupWidth + extraWidth + tileSpacing * 2);

            int targetInsertIndex = -1;
            if (targetTile != null && targetPlayerIndex >= 0 && targetPlayerIndex < 4)
            {
                int prev = (operatingPlayerIndex + 3) % 4;
                int opp = (operatingPlayerIndex + 2) % 4;
                int next = (operatingPlayerIndex + 1) % 4;

                if (targetPlayerIndex == prev) targetInsertIndex = 0;
                else if (targetPlayerIndex == opp) targetInsertIndex = 1;
                else if (targetPlayerIndex == next) targetInsertIndex = 3;
                else targetInsertIndex = tiles.Count;
            }

            List<MahjongTile> orderedTiles = new List<MahjongTile>(tiles);
            if (targetTile != null)
            {
                targetInsertIndex = Mathf.Clamp(targetInsertIndex, 0, orderedTiles.Count);
                orderedTiles.Insert(targetInsertIndex, targetTile);
            }

            for (int i = 0; i < orderedTiles.Count; i++)
            {
                GameObject tileObj = orderedTiles[i].GameObject;
                tileObj.transform.SetParent(anchor);

                float offset;
                bool isTarget = (targetTile != null && orderedTiles[i] == targetTile);

                if (isTarget)
                {
                    offset = groupOffset + i * (tileWidth + tileSpacing) + (tileHeight - tileWidth) / 2f;
                    tileObj.transform.localRotation = Quaternion.Euler(-180, 90, 0); // 明杠目标竖牌
                }
                else
                {
                    int shift = (targetTile != null && i > targetInsertIndex) ? 1 : 0;
                    offset = groupOffset + i * (tileWidth + tileSpacing) + shift * (tileHeight - tileWidth);
                    tileObj.transform.localRotation = Quaternion.Euler(-180, 0, 0);
                }

                tileObj.transform.localPosition = new Vector3(offset, 0, 0);
            }

            chowPungKongGroupCounts[operatingPlayerIndex]++;
        }

        /// <summary>
        /// 暗杠（4张，2张盖住）
        /// </summary>
        public void PlaceConcealedKong(int playerIndex, List<MahjongTile> tiles)
        {
            if (tiles == null || tiles.Count != 4)
            {
                Debug.LogWarning("Concealed Kong must have exactly 4 tiles.");
                return;
            }

            Transform anchor = chowPungKongTransforms[playerIndex];
            float tileWidth = MahjongConfig.TileWidth;
            float tileSpacing = MahjongConfig.TileSpacing;

            int groupCount = chowPungKongGroupCounts[playerIndex];
            float groupWidth = 4 * (tileWidth + tileSpacing);
            float groupOffset = groupCount * (groupWidth + tileSpacing * 2);

            for (int i = 0; i < tiles.Count; i++)
            {
                GameObject tileObj = tiles[i].GameObject;
                tileObj.transform.SetParent(anchor);

                float offset = groupOffset + i * (tileWidth + tileSpacing);
                tileObj.transform.localPosition = new Vector3(offset, 0, 0);

                if (i == 1 || i == 2)
                    tileObj.transform.localRotation = Quaternion.Euler(0, 0, 0); // 背面
                else
                    tileObj.transform.localRotation = Quaternion.Euler(-180, 0, 0); // 正面
            }

            chowPungKongGroupCounts[playerIndex]++;
        }

        /// <summary>
        /// 补杠（在 baseTile 上方叠加 tileToAdd）
        /// </summary>
        public void SupplementKong(MahjongTile tileToAdd, MahjongTile baseTile)
        {
            if (tileToAdd == null || baseTile == null)
            {
                Debug.LogWarning("SupplementKong requires both tileToAdd and baseTile.");
                return;
            }

            GameObject tileObj = tileToAdd.GameObject;
            tileObj.transform.SetParent(baseTile.GameObject.transform.parent);

            Vector3 basePos = baseTile.GameObject.transform.localPosition;
            float tileHeight = MahjongConfig.TileHeight;

            tileObj.transform.localPosition = new Vector3(basePos.x, tileHeight, basePos.z);
            tileObj.transform.localRotation = Quaternion.Euler(-180, 0, 0);
        }

        /// <summary>
        /// 胡牌展示
        /// </summary>
        public void PlaceWinTiles(int playerIndex, List<MahjongTile> tiles)
        {
            if (playerIndex < 0 || playerIndex >= winTransforms.Length || winTransforms[playerIndex] == null)
            {
                Debug.LogWarning($"Invalid player index {playerIndex} or null transform for Win.");
                return;
            }

            Transform anchor = winTransforms[playerIndex];
            float tileWidth = MahjongConfig.TileWidth;
            float tileSpacing = MahjongConfig.TileSpacing;

            for (int i = 0; i < tiles.Count; i++)
            {
                GameObject tileObj = tiles[i].GameObject;
                tileObj.transform.SetParent(anchor);
                float offset = i * (tileWidth + tileSpacing);
                tileObj.transform.localPosition = new Vector3(offset, 0, 0);
                tileObj.transform.localRotation = Quaternion.Euler(-180, -90, 0);
            }
        }

        public void ResetChowPungKongCount(int playerIndex)
        {
            if (chowPungKongGroupCounts.ContainsKey(playerIndex))
                chowPungKongGroupCounts[playerIndex] = 0;
        }
    }
}
