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
            LayerUtil.SetLayerRecursively(tileObj, LayerMask.NameToLayer("Default"));
            tileObj.transform.localScale = Vector3.one;
            Vector3 localPos = CalculateTilePosition(rackIndex, tileIndex,17);
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = GetTileRotation(rackIndex);

            tileObj.name = $"Mahjong_{rackIndex}_{tileIndex}";
            return true;
        }

        private Vector3 CalculateTilePosition(int rackIndex, int tileIndex, int totalTiles)
        {
            int col = tileIndex / 2;
            int row = 1 - (tileIndex % 2); // row: 0 for top, 1 for bottom (2-row stacking)

            float spacing = MahjongConfig.TileWidth + MahjongConfig.TileSpacing;

            // 总宽度 = tile数量 * 间隔 - 最后一个间隙
            float rowWidth = totalTiles * spacing - MahjongConfig.TileSpacing;

            // 中心居中：以中间 tile 为中心（tileIndex 从 0 开始）
            float start = - (spacing * (totalTiles - 1)) / 2f;

            // 是否反向排列（上家/对家）
            bool reverse = rackIndex == 1 || rackIndex == 2;

            // 计算偏移位置：正向 or 反向
            float pos = reverse
                ? start + (totalTiles - 1 - col) * spacing
                : start + col * spacing;

            // Stack 高度（第0排高度为 StackHeight，第二排为 0）
            float height = MahjongConfig.StackHeight * row;

            // 横向 or 纵向（玩家朝向）
            Vector3 localPos = (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, height, pos) // 左、右：纵向
                : new Vector3(pos, height, 0); // 下、上：横向

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