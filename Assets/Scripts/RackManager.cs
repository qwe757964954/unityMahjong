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

            MahjongDisplay display = tileObj.GetComponent<MahjongDisplay>();
            display.BindTile(tileData);

            tileObj.transform.SetParent(rack.transform, false);
            LayerUtil.SetLayerRecursively(tileObj, LayerMask.NameToLayer("Default"));
            tileObj.transform.localScale = Vector3.one;

            // 使用当前规则计算位置和旋转
            MahjongRule rule = GameDataManager.Instance.CurrentRule;
            Vector3 localPos = rule.CalculateTilePosition(rackIndex, tileIndex, rule.TilesPerPlayer);
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = rule.GetTileRotation(rackIndex);

            tileObj.name = $"Mahjong_{rackIndex}_{tileIndex}";
            return true;
        }



        public MahjongTile DrawTileFromRack()
        {
            int banker = GameDataManager.Instance.BankerIndex;
            MahjongRule rule = GameDataManager.Instance.CurrentRule;

            for (int i = 0; i < 4; i++)
            {
                int currentPlayer = (banker + i) % 4;
                Transform rack = anchorTransforms[currentPlayer].Find($"RackOffset_{currentPlayer}");
                if (rack == null || rack.childCount == 0) continue;

                // 抓最上面的一张牌
                Transform tileTransform = rack.GetChild(0);
                MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
                if (display == null || display.TileData == null) continue;

                MahjongTile tile = display.TileData;
                tileTransform.SetParent(null);
                tileTransform.gameObject.name = $"Drawn_{tileTransform.gameObject.name}";
                return tile;
            }

            Debug.LogWarning("No tiles left in any rack.");
            return null;
        }
    }

}