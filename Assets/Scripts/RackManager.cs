using UnityEngine;

namespace MahjongGame
{
    public class RackManager : MonoBehaviour
    {
        [SerializeField] private GameObject mahjongTable;
        [Header("Anchor Transforms (Down, Left, Up, Right)")]
        [SerializeField] private Transform[] anchorTransforms = new Transform[4];

        public void Initialize(GameObject table)
        {
            mahjongTable = table;
        }

        public GameObject[] CreateRackOffsets()
        {
            GameObject[] racks = new GameObject[4];
            for (int i = 0; i < anchorTransforms.Length; i++)
            {
                Transform anchor = anchorTransforms[i];
                Transform offset = anchor.Find($"RackOffset_{i}");
                racks[i] = offset.gameObject;
            }

            return racks;
        }

        public bool CreateTileOnRack(GameObject rack, int rackIndex, int tileIndex, MahjongTile tileData, EnhancedObjectPool tilePool)
        {
            GameObject tileObj = tilePool.Get();
            tileData.SetGameObject(tileObj);
            // ✅ 绑定数据到 MahjongDisplay
            MahjongDisplay display = tileObj.GetComponent<MahjongDisplay>();
            display.BindTile(tileData); // ✅ 这里绑定 MahjongTile 数据
            tileObj.transform.SetParent(rack.transform, false);

            Vector3 localPos = CalculateTilePosition(rackIndex, tileIndex);
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = GetTileRotation(rackIndex);

            tileObj.name = $"Mahjong_{rackIndex}_{tileIndex}";
            return true;
        }

        private Vector3 CalculateTilePosition(int rackIndex, int tileIndex)
        {
            int col = tileIndex / 2;
            int row = 1 - (tileIndex % 2);

            float rowWidth = 17 * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing) - MahjongConfig.TileSpacing;
            float start = -rowWidth / 2f;

            bool reverse = rackIndex == 1 || rackIndex == 2;
            float pos = reverse ? start + (16 - col) * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing)
                                : start + col * (MahjongConfig.TileWidth + MahjongConfig.TileSpacing);

            Vector3 localPos = (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, MahjongConfig.StackHeight * row, pos)
                : new Vector3(pos, MahjongConfig.StackHeight * row, 0);

            return localPos;
        }

        private Quaternion GetTileRotation(int playerIndex)
        {
            Quaternion rotation = playerIndex switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
            return rotation;
        }
        public MahjongTile DrawTileFromRack()
        {
            int banker = GameDataManager.Instance.BankerIndex;

            for (int i = 0; i < 4; i++)
            {
                int currentPlayer = (banker + i) % 4;
                Transform rack = anchorTransforms[currentPlayer].Find($"RackOffset_{currentPlayer}");
                if (rack == null || rack.childCount == 0)
                {
                    continue;
                }

                // 抓最上面的一张牌（第一个 child）
                Transform tileTransform = rack.GetChild(0);
                MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
                if (display == null || display.TileData == null)
                {
                    Debug.LogWarning($"MahjongDisplay component or TileData missing on rack {currentPlayer}.");
                    continue;
                }
                MahjongTile tile = display.TileData;
                tileTransform.SetParent(null); // 从 rack 分离出来
                tileTransform.gameObject.name = $"Drawn_{tileTransform.gameObject.name}";
                Debug.Log($"[RackManager] Player {currentPlayer} 抓牌成功。");
                return tile;
            }

            Debug.LogWarning("No tiles left in any rack.");
            return null;
        }
    }
    
}