using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MahjongGame;
using System;

namespace MahjongGame
{
    public class MahjongManager : MonoBehaviour
    {
        [Header("麻将牌设置")]
        public GameObject MahjongPrefab;
        [SerializeField] private GameObject mahjongTable;
        [Range(13, 100)]
        public int TilesPerRack = 34;

        [Header("动画设置")]
        public float AnimationDuration = 0.5f;

        public GameObject MahjongTable
        {
            get { return mahjongTable; }
            set { mahjongTable = value; }
        }

        private Animator tableAnimator;
        private ObjectPool tilePool;
        private List<GameObject> activeTiles = new List<GameObject>();
        private Dictionary<GameObject, MahjongTileData> tileDataMap = new Dictionary<GameObject, MahjongTileData>();
        private List<MahjongType> tileDeck = new List<MahjongType>();
        private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };
        private readonly Vector3[] rackPositions = {
            new Vector3(0.0175f, -0.057f, 0.429f),   // 下方
            new Vector3(0.429f, -0.057f, 0.0175f),   // 左方
            new Vector3(0.0175f, -0.057f, -0.429f),  // 上方
            new Vector3(-0.429f, -0.057f, 0.0175f)   // 右方
        };
        private GameObject[] lastRacks;
        private GameState currentState = GameState.Idle;

        private void Start()
        {
            tilePool = new ObjectPool(MahjongPrefab, MahjongTable.transform, 136);
            if (MahjongTable != null)
            {
                tableAnimator = MahjongTable.GetComponent<Animator>();
            }
        }

        public void InitializeMahjongTiles(int startIndex = 0)
        {
            StartCoroutine(GameLoop(startIndex));
        }

        public IEnumerator GameLoop(int startIndex)
        {
            while (true)
            {
                switch (currentState)
                {
                    case GameState.Idle:
                        yield return new WaitForSeconds(1f);
                        currentState = GameState.Shuffling;
                        break;
                    case GameState.Shuffling:
                        yield return StartCoroutine(ShuffleTilesRoutine());
                        currentState = GameState.Dealing;
                        break;
                    case GameState.Dealing:
                        yield return StartCoroutine(DealTilesRoutine(startIndex));
                        currentState = GameState.Playing;
                        break;
                    case GameState.Playing:
                        yield return null;
                        break;
                    case GameState.GameOver:
                        yield break;
                }
            }
        }

        private List<MahjongType> GenerateAllTiles()
        {
            List<MahjongType> allTiles = new List<MahjongType>();
            foreach (MahjongType type in System.Enum.GetValues(typeof(MahjongType)))
            {
                for (int i = 0; i < 4; i++)
                {
                    allTiles.Add(type);
                }
            }
            return allTiles;
        }

        private void ShuffleTiles(List<MahjongType> tiles)
        {
            System.Random rand = new System.Random();
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }
        }

        private IEnumerator ShuffleTilesRoutine()
        {
            tileDeck.Clear();

            List<MahjongType> allTiles = GenerateAllTiles();
            List<MahjongType> remainingTiles = new List<MahjongType>(allTiles);
            foreach (var tile in activeTiles)
            {
                if (tileDataMap.TryGetValue(tile, out var tileData))
                {
                    remainingTiles.Remove(tileData.Type);
                }
            }

            tileDeck = remainingTiles;
            ShuffleTiles(tileDeck);
            yield return new WaitForSeconds(AnimationDuration);
            Debug.Log("Tiles shuffled! Remaining tiles in deck: " + tileDeck.Count);
        }

        private void CreateTilesOnRacks(List<MahjongType> tiles, int startIndex)
        {
            Transform tableTransform = MahjongTable?.transform;
            if (tableTransform == null)
            {
                Debug.LogError("找不到麻将桌！");
                return;
            }

            GameObject[] racks = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = tableTransform.Find(anchorNames[i]);
                if (anchor == null)
                {
                    Debug.LogError($"未找到锚点: {anchorNames[i]}");
                    return;
                }

                Transform offset = anchor.childCount > 0 && anchor.GetChild(0).name == $"RackOffset_{i}" ? anchor.GetChild(0) : new GameObject($"RackOffset_{i}").transform;
                offset.SetParent(anchor, false);
                offset.localPosition = rackPositions[i];
                racks[i] = offset.gameObject;
            }

            // 顺时针顺序：东(0)->南(1)->西(2)->北(3)
            int[] rackOrder = new int[4];
            for (int i = 0; i < 4; i++)
            {
                rackOrder[i] = (startIndex + i) % 4;
            }
            int[] rackTileCount = new int[4];
            int tilesPerPlayer = tiles.Count / 4;
            float tileWidth = 0.035f, tileSpacing = 0.002f, stackHeight = 0.021f;

            int tileIndex = 0;
            foreach (int rackIndex in rackOrder)
            {
                for (int i = 0; i < tilesPerPlayer; i++)
                {
                    int idx = rackTileCount[rackIndex];
                    int col = idx / 2;
                    int row = 1 - (idx % 2);
                    if (col >= 17) continue;

                    GameObject tile = tilePool.Get();
                    tile.transform.SetParent(racks[rackIndex].transform, false);

                    float rowWidth = 17 * (tileWidth + tileSpacing) - tileSpacing;
                    float start = -rowWidth / 2f;

                    bool reverse = rackIndex == 1 || rackIndex == 2;
                    float pos = reverse ? start + (16 - col) * (tileWidth + tileSpacing)
                                         : start + col * (tileWidth + tileSpacing);

                    Vector3 localPos = (rackIndex == 1 || rackIndex == 3)
                        ? new Vector3(0, stackHeight * row, pos)
                        : new Vector3(pos, stackHeight * row, 0);

                    tile.transform.localPosition = localPos;
                    tile.transform.localRotation = (rackIndex == 1 || rackIndex == 3)
                        ? Quaternion.Euler(0, 90, 0)
                        : Quaternion.identity;

                    tile.name = $"Mahjong_{rackIndex}_{row}_{col}";
                    MahjongDisplay display = tile.GetComponent<MahjongDisplay>();
                    display.SetType(tiles[tileIndex]);
                    tileDataMap[tile] = new MahjongTileData(tiles[tileIndex]);
                    activeTiles.Add(tile);
                    tileIndex++;
                    rackTileCount[rackIndex]++;
                }
            }

            lastRacks = racks;
        }

        private IEnumerator DealTilesRoutine(int startIndex)
        {
            if (activeTiles.Count == 0)
            {
                List<MahjongType> allTiles = GenerateAllTiles();
                ShuffleTiles(allTiles);
                CreateTilesOnRacks(allTiles, startIndex);
                tileDeck = new List<MahjongType>(allTiles);
                tileDeck.RemoveRange(0, 4 * TilesPerRack);
            }

            Debug.Log("Tiles dealt!");
            yield break;
        }

        private Vector3 CalculateTilePosition(int rackIndex, int tileIndex)
        {
            float tileWidth = 0.035f;
            float tileSpacing = 0.002f;
            float rowWidth = TilesPerRack * (tileWidth + tileSpacing) - tileSpacing;
            float start = -rowWidth / 2f;
            float pos = start + (tileIndex % TilesPerRack) * (tileWidth + tileSpacing);

            return (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, 0, pos)
                : new Vector3(pos, 0, 0);
        }

        private Quaternion GetTileRotation(int rackIndex)
        {
            return rackIndex switch
            {
                1 => Quaternion.Euler(0, 90, 0),
                2 => Quaternion.Euler(0, 180, 0),
                3 => Quaternion.Euler(0, -90, 0),
                _ => Quaternion.identity
            };
        }

        private void ClearTiles()
        {
            foreach (GameObject tile in activeTiles)
            {
                tilePool.Return(tile);
            }
            activeTiles.Clear();
            tileDataMap.Clear();
            tileDeck.Clear();
        }

        public MahjongTileData DrawTile(Transform handAnchor)
        {
            if (tileDeck.Count == 0) return null;

            GameObject tile = tilePool.Get();
            tile.transform.SetParent(handAnchor);
            tile.transform.localPosition = CalculateTilePosition(0, activeTiles.Count % TilesPerRack);
            MahjongType type = tileDeck[0];
            tileDeck.RemoveAt(0);

            MahjongDisplay display = tile.GetComponent<MahjongDisplay>();
            display.SetType(type);
            display.PlayDrawAnimation(tile.transform.localPosition);
            MahjongTileData data = new MahjongTileData(type);
            tileDataMap[tile] = data;
            activeTiles.Add(tile);

            return data;
        }

        public void DiscardTile(GameObject tile, Transform discardAnchor)
        {
            if (activeTiles.Contains(tile))
            {
                tile.transform.SetParent(discardAnchor);
                tile.transform.DOLocalMove(CalculateTilePosition(0, discardAnchor.childCount), AnimationDuration);
                activeTiles.Remove(tile);
                tileDataMap.Remove(tile);
            }
        }

        public void PlayRackAnimation()
        {
            if (tableAnimator == null && MahjongTable != null)
            {
                tableAnimator = MahjongTable.GetComponent<Animator>();
            }
            if (tableAnimator != null)
            {
                tableAnimator.SetFloat("Blend", 1f);
            }
            else
            {
                Debug.LogWarning("Table Animator not found on MahjongTable!");
            }
        }

        // Added method to access activeTiles
        public List<GameObject> GetActiveTiles()
        {
            return new List<GameObject>(activeTiles);
        }
    }
}