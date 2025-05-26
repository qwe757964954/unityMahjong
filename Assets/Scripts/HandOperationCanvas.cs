using UnityEngine;
using UnityEngine.UI;
using MahjongGame;
using System.Collections;
using System.Collections.Generic;

namespace MahjongGame
{
    public class HandOperationCanvas : MonoBehaviour
    {
        [SerializeField] private Button ShuffleButton;
        [SerializeField] private Button SendHandCardButton; // Added for sending hand cards
        [SerializeField] private Button DrawButton;
        [SerializeField] private Button DiscardButton;
        [SerializeField] private Transform HandAnchor;
        [SerializeField] private Transform DiscardAnchor;
        [SerializeField] private GameObject RiceNumberInput; // Input field for dice numbers

        private MahjongManager mahjongManager;
        private InputField riceInputField;
        private GameObject selectedTile;
        private Transform[] anchorTransforms; // For hand card anchors
        private List<GameObject> allTiles; // Tiles to deal from
        private int[] playerCardCounts; // Track cards per player

        // Constants for dealing hand cards
        private const int INITIAL_HAND_COUNT = 13;
        private const int EAST_EXTRA_CARD = 14;
        private const float OFFSET_Y = -0.05f; // Adjust as needed
        private const float TILE_WIDTH = 0.035f;
        private const float TILE_SPACING = 0.002f;
        private const float WAIT_DURATION = 0.1f;

        private void Awake()
        {
            mahjongManager = FindObjectOfType<MahjongManager>();
            riceInputField = RiceNumberInput?.GetComponent<InputField>();

            // Validate required components
            if (ShuffleButton == null) Debug.LogError("ShuffleButton is null");
            if (SendHandCardButton == null) Debug.LogError("SendHandCardButton is null");
            if (DrawButton == null) Debug.LogError("DrawButton is null");
            if (DiscardButton == null) Debug.LogError("DiscardButton is null");
            if (RiceNumberInput == null) Debug.LogError("RiceNumberInput is null");
            if (mahjongManager == null) Debug.LogError("MahjongManager is null");

            if (ShuffleButton == null || SendHandCardButton == null || DrawButton == null || DiscardButton == null || RiceNumberInput == null || mahjongManager == null)
            {
                Debug.LogError("Missing required components or references in HandOperationCanvas.");
                enabled = false;
                return;
            }

            // Setup button listeners
            SetupButton(ShuffleButton, ShuffleAndSetDice);
            SetupButton(SendHandCardButton, SendHandCardsByDice);
            DrawButton.onClick.AddListener(DrawTile);
            DiscardButton.onClick.AddListener(DiscardTile);
        }

        private void SetupButton(Button button, System.Func<IEnumerator> coroutineMethod)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => StartCoroutine(coroutineMethod()));
            }
        }

        private IEnumerator ShuffleAndSetDice()
        {
            if (mahjongManager == null) yield break;
            mahjongManager.PlayRackAnimation();
            yield return new WaitForSeconds(0.5f);

            // Dice logic can be reintroduced later if needed
            (int n1, int n2) = ParseDiceInput();
            // if (diceController != null)
            // {
            //     diceController.SetDiceNumbers(n1, n2);
            // }

            mahjongManager.InitializeMahjongTiles();
        }

        private IEnumerator SendHandCardsByDice()
        {
            if (mahjongManager == null || mahjongManager.MahjongTable == null) yield break;

            (int n1, int n2) = ParseDiceInput();
            int startIndex = (n1 + n2 - 1) % 4;

            InitializeAnchors();
            allTiles = mahjongManager.GetActiveTiles(); // Get tiles from MahjongManager
            if (allTiles == null || allTiles.Count < 54) // 4 players * 13 + 1 extra for East
            {
                Debug.LogError("Not enough tiles to deal hand cards!");
                yield break;
            }

            playerCardCounts = new int[4];

            int[] handTotals = InitializeHandTotals(startIndex);

            // Deal three sets of 4 cards each
            for (int round = 0; round < 3; round++)
            {
                for (int p = 0; p < 4; p++)
                {
                    int player = (startIndex + p) % 4;
                    yield return DealHandCards(player, 4, handTotals[player]);
                }
            }

            // Deal one more card to each player
            for (int p = 0; p < 4; p++)
            {
                int player = (startIndex + p) % 4;
                yield return DealHandCards(player, 1, handTotals[player]);
            }

            // Deal one extra card to the starting player (East)
            yield return DealHandCards(startIndex, 1, handTotals[startIndex]);

            Debug.Log("Hand cards dealt!");
        }

        private (int, int) ParseDiceInput()
        {
            if (riceInputField == null) return (1, 1);
            string input = riceInputField.text.Trim();
            string[] nums = input.Split(new char[] { ' ', ',', ';', '，' }, System.StringSplitOptions.RemoveEmptyEntries);
            int n1 = 1, n2 = 1;
            if (nums.Length >= 2)
            {
                int.TryParse(nums[0], out n1);
                int.TryParse(nums[1], out n2);
            }
            return (n1, n2);
        }

        private void InitializeAnchors()
        {
            string[] anchors = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };
            anchorTransforms = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = mahjongManager.MahjongTable.transform.Find(anchors[i]);
                if (anchor == null) continue;

                Transform handOffset = anchor.Find($"HandOffset_{i}") ?? CreateHandOffset(anchor, i);
                anchorTransforms[i] = handOffset;
            }
        }

        private Transform CreateHandOffset(Transform anchor, int i)
        {
            Transform rackOffset = anchor.childCount > 0 && anchor.GetChild(0).name == $"RackOffset_{i}" ? anchor.GetChild(0) : anchor;
            Vector3 offsetDirWorld = GetOffsetDirection(i);
            Vector3 offsetDirLocal = anchor.InverseTransformDirection(offsetDirWorld);
            Transform newHand = new GameObject($"HandOffset_{i}").transform;
            newHand.SetParent(anchor, false);

            Vector3 basePos = rackOffset.localPosition + offsetDirLocal;
            basePos.y = OFFSET_Y;
            newHand.localPosition = basePos;

            Quaternion rot = GetRotation(i);
            newHand.localRotation = rot;

            return newHand;
        }

        private Vector3 GetOffsetDirection(int i)
        {
            return i switch
            {
                0 => new Vector3(0, 0, 0.08f),   // 东位向北
                1 => new Vector3(0.08f, 0, 0),   // 南位向东
                2 => new Vector3(0, 0, -0.08f),  // 西位向南
                3 => new Vector3(-0.08f, 0, 0),  // 北位向西
                _ => Vector3.zero
            };
        }

        private Quaternion GetRotation(int i)
        {
            return i switch
            {
                1 => Quaternion.Euler(0, 90, 0),    // 南位
                2 => Quaternion.Euler(0, 180, 0),   // 西位
                3 => Quaternion.Euler(0, -90, 0),   // 北位
                _ => Quaternion.identity            // 东位
            };
        }

        private int[] InitializeHandTotals(int startIndex)
        {
            int[] handTotals = new int[4] { INITIAL_HAND_COUNT, INITIAL_HAND_COUNT, INITIAL_HAND_COUNT, INITIAL_HAND_COUNT };
            handTotals[startIndex] = EAST_EXTRA_CARD;
            return handTotals;
        }

        private IEnumerator DealHandCards(int player, int count, int totalCards)
        {
            Transform anchor = anchorTransforms[player];
            if (anchor == null || playerCardCounts[player] >= totalCards) yield break;

            float rowWidth = totalCards * (TILE_WIDTH + TILE_SPACING) - TILE_SPACING;
            float start = -rowWidth / 2f;

            for (int j = 0; j < count && allTiles.Count > 0; j++)
            {
                GameObject tile = allTiles[0];
                allTiles.RemoveAt(0);
                tile.transform.SetParent(anchor);
                float pos = start + playerCardCounts[player] * (TILE_WIDTH + TILE_SPACING);
                tile.transform.position = anchor.position + anchor.right * pos;
                tile.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                playerCardCounts[player]++;
                yield return new WaitForSeconds(WAIT_DURATION);
            }
        }

        private void DrawTile()
        {
            MahjongTileData tileData = mahjongManager.DrawTile(HandAnchor);
            if (tileData != null)
            {
                Debug.Log($"Drew tile: {tileData.Suit} {tileData.Number}");
            }
        }

        private void DiscardTile()
        {
            if (selectedTile != null)
            {
                mahjongManager.DiscardTile(selectedTile, DiscardAnchor);
                selectedTile = null;
            }
        }

        public void SelectTile(GameObject tile)
        {
            selectedTile = tile;
        }
    }
}