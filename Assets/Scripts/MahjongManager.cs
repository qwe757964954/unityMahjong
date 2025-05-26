using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class MahjongManager : MonoBehaviour
{
    [Header("麻将牌设置")]
    public GameObject mahjongPrefab;
    public GameObject mahjongTable;
    [Range(13, 100)]
    public int tilesPerRack = 34;

    public enum MahjongType
    {
        Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8, Dot9,
        Bamboo1, Bamboo2, Bamboo3, Bamboo4, Bamboo5, Bamboo6, Bamboo7, Bamboo8, Bamboo9,
        Character1, Character2, Character3, Character4, Character5, Character6, Character7, Character8, Character9,
        Wind_East, Wind_South, Wind_West, Wind_North,
        Dragon_Red, Dragon_Green, Dragon_White
    }

    private List<GameObject> mahjongTiles = new();
    private Dictionary<GameObject, MahjongType> tileTypeMap = new();
    private GameObject[] lastRacks;
    private Animator tableAnimator;

    private readonly string[] anchorNames = { "Anchor_Down", "Anchor_Left", "Anchor_Up", "Anchor_Right" };
    private readonly Vector3[] rackPositions = {
        new(0.0175f, -0.057f, 0.429f),   // 下方
        new(0.429f, -0.057f, 0.0175f),   // 左方
        new(0.0175f, -0.057f, -0.429f),  // 上方
        new(-0.429f, -0.057f, 0.0175f)   // 右方
    };

    void Start()
    {
        if (mahjongTable == null)
            mahjongTable = GameObject.Find("Mahjong table_009 1");
        InitializeMahjongTiles();
    }

    public void InitializeMahjongTiles()
    {
        if (mahjongPrefab == null)
        {
            Debug.LogError("麻将牌预制体未设置！");
            return;
        }

        ClearMahjongTiles();

        List<MahjongType> allTiles = new();
        foreach (MahjongType type in System.Enum.GetValues(typeof(MahjongType)))
            for (int i = 0; i < 4; i++) allTiles.Add(type);

        ShuffleTiles(allTiles);
        CreateTilesOnRacks(allTiles);
    }

    private void ShuffleTiles(List<MahjongType> tiles)
    {
        System.Random rand = new();
        for (int i = tiles.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }

    private void CreateTilesOnRacks(List<MahjongType> tiles)
    {
        Transform tableTransform = mahjongTable?.transform;
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
            // offset.localRotation = Quaternion.identity;
            racks[i] = offset.gameObject;
        }

        int[] rackOrder = { 0, 2, 3, 1 }; // 下 -> 上 -> 右 -> 左
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

                GameObject tile = Instantiate(mahjongPrefab);
                tile.transform.SetParent(racks[rackIndex].transform, false);

                float rowWidth = 17 * (tileWidth + tileSpacing) - tileSpacing;
                float start = -rowWidth / 2;

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
                ApplyTileTexture(tile, tiles[tileIndex]);
                mahjongTiles.Add(tile);
                tileTypeMap[tile] = tiles[tileIndex++];
                rackTileCount[rackIndex]++;
            }
        }

        lastRacks = racks;
    }

    private void ApplyTileTexture(GameObject tile, MahjongType type)
    {
        var display = tile.GetComponent<MahjongDisplay>();
        if (display == null)
        {
            display = tile.AddComponent<MahjongDisplay>();
        }
        // 自动赋值 meshFilter
        if (display.meshFilter == null)
        {
            display.meshFilter = tile.GetComponent<MeshFilter>();
        }
        display.SetType(type);
    }

    public void ClearMahjongTiles()
    {
        foreach (GameObject tile in mahjongTiles)
            if (tile != null) Destroy(tile);
        mahjongTiles.Clear();
        tileTypeMap.Clear();
    }

    public MahjongType GetTileType(GameObject tile) =>
        tileTypeMap.TryGetValue(tile, out var type) ? type : MahjongType.Dot1;

    public void PlayRackAnimation()
    {
        tableAnimator = mahjongTable.GetComponent<Animator>();
        tableAnimator.SetFloat("Blend", 1f);
    }
}
