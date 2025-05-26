using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandOperationCanvas : MonoBehaviour
{
    private const float OFFSET_Y = -0.046f;
    private const float TILE_WIDTH = 0.035f;
    private const float TILE_SPACING = 0.002f;
    private const float WAIT_DURATION = 0.08f;
    private const int INITIAL_HAND_COUNT = 13;
    private const int EAST_EXTRA_CARD = 14;

    [SerializeField] private GameObject shuffleButton;
    [SerializeField] private GameObject sendHandCardButton;
    [SerializeField] private GameObject riceNumberInput;

    private MahjongManager mahjongManager;
    private DiceController diceController;
    private InputField riceInputField;

    private Transform[] anchorTransforms;
    private List<GameObject> allTiles;
    private int[] playerCardCounts;

    private void Awake()
    {
        mahjongManager = FindObjectOfType<MahjongManager>();
        diceController = FindObjectOfType<DiceController>();
        riceInputField = riceNumberInput?.GetComponent<InputField>();

        if (shuffleButton == null) Debug.LogError("shuffleButton is null");
        if (sendHandCardButton == null) Debug.LogError("sendHandCardButton is null");
        if (riceNumberInput == null) Debug.LogError("riceNumberInput is null");
        if (mahjongManager == null) Debug.LogError("mahjongManager is null");
        if (diceController == null) Debug.LogError("diceController is null");
        if (shuffleButton == null || sendHandCardButton == null || riceNumberInput == null || mahjongManager == null || diceController == null)
        {
            Debug.LogError("Missing required components or references in HandOperationCanvas.");
            enabled = false;
            return;
        }

        SetupButton(shuffleButton, ShuffleAndSetDice);
        SetupButton(sendHandCardButton, SendHandCardsByDice);
    }

    private void SetupButton(GameObject buttonObj, System.Func<IEnumerator> coroutineMethod)
    {
        if (buttonObj != null)
        {
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => StartCoroutine(coroutineMethod()));
            }
        }
    }

    private IEnumerator ShuffleAndSetDice()
    {
        if (mahjongManager == null) yield break;
        mahjongManager.PlayRackAnimation();
        yield return new WaitForSeconds(0.5f);

        (int n1, int n2) = ParseDiceInput();
        if (diceController != null)
        {
            // diceController.SetDiceNumbers(n1, n2);
        }
    }

    private IEnumerator SendHandCardsByDice()
    {
        if (mahjongManager == null || mahjongManager.mahjongTable == null) yield break;

        (int n1, int n2) = ParseDiceInput();
        int startIndex = (n1 + n2 - 1) % 4;

        InitializeAnchors();
        allTiles = GetMahjongTiles();
        playerCardCounts = new int[4];

        int[] handTotals = InitializeHandTotals(startIndex);

        for (int round = 0; round < 3; round++)
        {
            for (int p = 0; p < 4; p++)
            {
                int player = (startIndex + p) % 4;
                yield return DealHandCards(player, 4, handTotals[player]);
            }
        }

        for (int p = 0; p < 4; p++)
        {
            int player = (startIndex + p) % 4;
            yield return DealHandCards(player, 1, handTotals[player]);
        }

        yield return DealHandCards(startIndex, 1, handTotals[startIndex]);
    }

    private (int n1, int n2) ParseDiceInput()
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
            Transform anchor = mahjongManager.mahjongTable.transform.Find(anchors[i]);
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

    private List<GameObject> GetMahjongTiles()
    {
        return mahjongManager.GetType().GetField("mahjongTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(mahjongManager) as List<GameObject> ?? new List<GameObject>();
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
}