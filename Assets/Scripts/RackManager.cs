using UnityEngine;

namespace MahjongGame
{
    public class RackManager : MonoBehaviour
    {
        [SerializeField] private GameObject mahjongTable;
        private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };

        public void Initialize(GameObject table)
        {
            mahjongTable = table;
            if (mahjongTable == null)
            {
                Debug.LogError("MahjongTable is not assigned in RackManager. Disabling component.");
                enabled = false;
            }
        }

        public GameObject[] CreateRackOffsets()
        {
            if (!enabled)
            {
                Debug.LogError("RackManager is disabled.");
                return new GameObject[4];
            }

            Transform tableTransform = mahjongTable?.transform;
            if (tableTransform == null)
            {
                Debug.LogError("Mahjong table not found!");
                return new GameObject[4];
            }

            GameObject[] racks = new GameObject[4];
            bool anyRackCreated = false;

            for (int i = 0; i < 4; i++)
            {
                Transform anchor = tableTransform.Find(anchorNames[i]);
                if (anchor == null)
                {
                    Debug.LogError($"Anchor not found: {anchorNames[i]} on MahjongTable.");
                    continue;
                }

                Transform offset = anchor.Find($"RackOffset_{i}");
                if (offset == null)
                {
                    GameObject rackOffset = new GameObject($"RackOffset_{i}");
                    offset = rackOffset.transform;
                    offset.SetParent(anchor, false);
                    offset.localPosition = MahjongConfig.RackPositions[i];
                    Debug.Log($"Created RackOffset_{i} at {anchorNames[i]} with position {offset.localPosition}");
                }

                racks[i] = offset.gameObject;
                anyRackCreated = true;
            }

            if (!anyRackCreated)
            {
                Debug.LogError("No racks created. Check MahjongTable hierarchy for anchors.");
            }

            for (int i = 0; i < 4; i++)
            {
                Debug.Log($"Rack {i}: {(racks[i] != null ? racks[i].name : "null")}");
            }

            return racks;
        }

        public bool CreateTileOnRack(GameObject rack, int rackIndex, int tileIndex, MahjongTile tileData, EnhancedObjectPool tilePool)
        {
            if (!enabled)
            {
                Debug.LogError("RackManager is disabled.");
                return false;
            }

            if (tilePool == null)
            {
                Debug.LogError("TilePool is not initialized.");
                return false;
            }

            if (rack == null)
            {
                Debug.LogError($"Rack {rackIndex} is null. Cannot create tile.");
                return false;
            }

            GameObject tileObj = tilePool.Get();
            if (tileObj == null)
            {
                Debug.LogWarning("Failed to retrieve tile from pool.");
                return false;
            }

            tileData.SetGameObject(tileObj);
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

            Debug.Log($"Rack {rackIndex}, Tile {tileIndex}: Position = {localPos}");
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
            Debug.Log($"Rack {playerIndex}: Rotation = {rotation.eulerAngles}");
            return rotation;
        }
    }
}