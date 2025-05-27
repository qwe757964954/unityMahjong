
// ========== Enhanced MahjongManager ==========
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MahjongGame
{
    /// <summary>
    /// Enhanced Mahjong Manager with improved architecture and error handling
    /// </summary>
    public class EnhancedMahjongManager : MonoBehaviour
    {
        [Header("Mahjong Setup")]
        public GameObject mahjongPrefab;
        [SerializeField] private GameObject mahjongTable;
        
        [Header("Game Rules")]
        public IMahjongRule gameRule = new StandardMahjongRule();
        
        [Header("Components")]
        public TileAnimator tileAnimator;

        // Properties
        public GameObject MahjongTable
        {
            get { return mahjongTable; }
            set { mahjongTable = value; }
        }

        // Private fields
        private EnhancedObjectPool tilePool;
        private List<MahjongTile> activeTiles = new List<MahjongTile>();
        private List<MahjongTile> tileDeck = new List<MahjongTile>();
        private GameState currentState = GameState.Idle;
        private Animator tableAnimator;

        // Constants
        private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };
        private readonly Vector3[] rackPositions = {
            new Vector3(0.0175f, -0.057f, 0.429f),   // Down
            new Vector3(0.429f, -0.057f, 0.0175f),   // Left
            new Vector3(0.0175f, -0.057f, -0.429f),  // Up
            new Vector3(-0.429f, -0.057f, 0.0175f)   // Right
        };

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Validate required references
            if (mahjongPrefab == null)
            {
                Debug.LogError("MahjongPrefab is not assigned!");
                return;
            }

            if (mahjongTable == null)
            {
                Debug.LogError("MahjongTable is not assigned!");
                return;
            }

            // Initialize object pool
            tilePool = new EnhancedObjectPool(mahjongPrefab, mahjongTable.transform, MahjongConfig.DefaultPoolSize);
            
            // Get table animator
            tableAnimator = mahjongTable.GetComponent<Animator>();
            
            // Get or create tile animator
            if (tileAnimator == null)
            {
                tileAnimator = GetComponent<TileAnimator>() ?? gameObject.AddComponent<TileAnimator>();
            }
        }

        /// <summary>
        /// Initialize the mahjong game with tiles
        /// </summary>
        public IEnumerator InitializeGameRoutine()
        {
            try
            {
                currentState = GameState.Shuffling;
                yield return StartCoroutine(ShuffleTilesRoutine());
                
                currentState = GameState.Dealing;
                yield return StartCoroutine(DealTilesRoutine());
                
                currentState = GameState.Playing;
                Debug.Log("Game initialized successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize game: {e.Message}");
                currentState = GameState.Idle;
            }
        }

        private IEnumerator ShuffleTilesRoutine()
        {
            ClearTiles();
            
            // Generate all tiles using rule
            gameRule.InitializeDeck(tileDeck);
            
            // Shuffle the deck
            ShuffleTiles(tileDeck);
            
            yield return new WaitForSeconds(MahjongConfig.AnimationDuration);
            Debug.Log($"Tiles shuffled! Total tiles: {tileDeck.Count}");
        }

        private void ShuffleTiles(List<MahjongTile> tiles)
        {
            var random = new System.Random();
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }
        }

        private IEnumerator DealTilesRoutine()
        {
            if (tileDeck.Count == 0)
            {
                Debug.LogError("No tiles in deck to deal!");
                yield break;
            }

            CreateTilesOnRacks();
            yield return new WaitForSeconds(MahjongConfig.AnimationDuration);
            Debug.Log("Tiles dealt to racks!");
        }

        private void CreateTilesOnRacks()
        {
            Transform tableTransform = mahjongTable?.transform;
            if (tableTransform == null)
            {
                Debug.LogError("Mahjong table not found!");
                return;
            }

            // Create rack offsets
            GameObject[] racks = CreateRackOffsets(tableTransform);
            
            // Deal tiles to racks
            int tilesPerRack = gameRule.TilesPerPlayer;
            int tileIndex = 0;

            for (int rackIndex = 0; rackIndex < 4 && tileIndex < tileDeck.Count; rackIndex++)
            {
                for (int i = 0; i < tilesPerRack && tileIndex < tileDeck.Count; i++)
                {
                    CreateTileOnRack(racks[rackIndex], rackIndex, i, tileDeck[tileIndex]);
                    tileIndex++;
                }
            }
        }

        private GameObject[] CreateRackOffsets(Transform tableTransform)
        {
            GameObject[] racks = new GameObject[4];
            
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = tableTransform.Find(anchorNames[i]);
                if (anchor == null)
                {
                    Debug.LogError($"Anchor not found: {anchorNames[i]}");
                    continue;
                }

                Transform offset = anchor.Find($"RackOffset_{i}");
                if (offset == null)
                {
                    GameObject rackOffset = new GameObject($"RackOffset_{i}");
                    offset = rackOffset.transform;
                    offset.SetParent(anchor, false);
                    offset.localPosition = rackPositions[i];
                }
                
                racks[i] = offset.gameObject;
            }
            
            return racks;
        }

        private void CreateTileOnRack(GameObject rack, int rackIndex, int tileIndex, MahjongTile tileData)
        {
            GameObject tileObj = tilePool.Get();
            if (tileObj == null) return;

            // Set up the tile
            tileData.GameObject = tileObj;
            tileObj.transform.SetParent(rack.transform, false);
            
            // Calculate position
            Vector3 localPos = CalculateTilePosition(rackIndex, tileIndex);
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = GetTileRotation(rackIndex);
            
            // Set tile name and display
            tileObj.name = $"Mahjong_{rackIndex}_{tileIndex}";
            
            // Update display
            var display = tileObj.GetComponent<MahjongDisplay>();
            if (display != null)
            {
                display.SetType(tileData.Type);
            }
            
            activeTiles.Add(tileData);
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

            return (rackIndex == 1 || rackIndex == 3)
                ? new Vector3(0, MahjongConfig.StackHeight * row, pos)
                : new Vector3(pos, MahjongConfig.StackHeight * row, 0);
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

        /// <summary>
        /// Draw a tile from the deck
        /// </summary>
        public MahjongTile DrawTile(Transform handAnchor)
        {
            if (tileDeck.Count == 0)
            {
                Debug.LogWarning("No more tiles in deck to draw!");
                return null;
            }

            try
            {
                GameObject tileObj = tilePool.Get();
                if (tileObj == null) return null;

                MahjongTile tile = tileDeck[0];
                tileDeck.RemoveAt(0);
                
                tile.GameObject = tileObj;
                tile.SetParent(handAnchor);
                tile.SetLocalPosition(Vector3.zero);
                
                if (tileAnimator != null)
                {
                    tileAnimator.AnimateDraw(tile, tile.GameObject.transform.localPosition);
                }
                
                activeTiles.Add(tile);
                return tile;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to draw tile: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Discard a tile to the discard area
        /// </summary>
        public bool DiscardTile(MahjongTile tile, Transform discardAnchor)
        {
            if (tile?.GameObject == null || !activeTiles.Contains(tile))
            {
                Debug.LogWarning("Invalid tile to discard!");
                return false;
            }

            try
            {
                tile.SetParent(discardAnchor);
                Vector3 discardPos = Vector3.zero; // Calculate appropriate discard position
                
                if (tileAnimator != null)
                {
                    tileAnimator.AnimateDiscard(tile, discardPos);
                }
                
                activeTiles.Remove(tile);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to discard tile: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Play the rack animation
        /// </summary>
        public void PlayRackAnimation()
        {
            try
            {
                if (tableAnimator != null)
                {
                    tableAnimator.SetFloat("Blend", 1f);
                }
                else
                {
                    Debug.LogWarning("Table Animator not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Animation playback failed: {e.Message}");
            }
        }

        private void ClearTiles()
        {
            foreach (var tile in activeTiles)
            {
                if (tile?.GameObject != null)
                {
                    tilePool.Return(tile.GameObject);
                }
            }
            activeTiles.Clear();
            tileDeck.Clear();
        }

        /// <summary>
        /// Get all active tiles (for dealing hand cards)
        /// </summary>
        public List<GameObject> GetActiveTiles()
        {
            return activeTiles.Where(t => t?.GameObject != null).Select(t => t.GameObject).ToList();
        }

        /// <summary>
        /// Get tiles as MahjongTile objects
        /// </summary>
        public List<MahjongTile> GetActiveMahjongTiles()
        {
            return new List<MahjongTile>(activeTiles);
        }
    }
}