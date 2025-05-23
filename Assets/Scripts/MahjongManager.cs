using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class MahjongManager : MonoBehaviour
{
    // 麻将牌预制体引用
    public GameObject mahjongPrefab;

    // 麻将桌引用
    public GameObject mahjongTable;

    // 每个牌夹的麻将牌数量
    [Range(13, 100)]
    public int tilesPerRack = 34; // 每个牌夹基本分配34张牌
    
    // 麻将牌类型
    public enum MahjongType
    {
        // 筒子
        Dot1, Dot2, Dot3, Dot4, Dot5, Dot6, Dot7, Dot8, Dot9,
        // 条子
        Bamboo1, Bamboo2, Bamboo3, Bamboo4, Bamboo5, Bamboo6, Bamboo7, Bamboo8, Bamboo9,
        // 万子
        Character1, Character2, Character3, Character4, Character5, Character6, Character7, Character8, Character9,
        // 风牌 (东南西北)
        Wind_East, Wind_South, Wind_West, Wind_North,
        // 箭牌 (中发白)
        Dragon_Red, Dragon_Green, Dragon_White
    }

    // 存储所有麻将牌的列表
    private List<GameObject> mahjongTiles = new List<GameObject>();
    
    // 麻将牌实例与类型的映射
    private Dictionary<GameObject, MahjongType> tileTypeMap = new Dictionary<GameObject, MahjongType>();

    [Header("Animation Settings")]
    public bool enableAnimation = true;
    public float animationDuration = 1.0f;
    public float animationHeight = 0.5f;
    public Ease animationEase = Ease.OutBack;

    void Start()
    {
        // 查找麻将桌
        if (mahjongTable == null)
        {
            mahjongTable = GameObject.Find("Mahjong table_009 1");
        }
        
        // 初始化麻将牌
        InitializeMahjongTiles();
    }

    // 初始化麻将牌
    public void InitializeMahjongTiles()
    {
        // 确保有麻将牌预制体
        if (mahjongPrefab == null)
        {
            Debug.LogError("麻将牌预制体未设置！请在Inspector中设置mahjongPrefab");
            return;
        }

        // 清除现有的麻将牌
        ClearMahjongTiles();
        
        // 创建标准麻将牌集 - 共136张 (34种类型，每种4张)
        List<MahjongType> allTiles = new List<MahjongType>();
        
        foreach (MahjongType type in System.Enum.GetValues(typeof(MahjongType)))
        {
            for (int i = 0; i < 4; i++) // 每种类型4张
            {
                allTiles.Add(type);
            }
        }
        
        // 打乱麻将牌（洗牌）
        ShuffleTiles(allTiles);
        
        // 输出牌数量信息用于调试
        Debug.Log("总共创建了 " + allTiles.Count + " 张麻将牌");
        
        // 创建麻将牌并摆放在4个牌夹上
        CreateTilesOnRacks(allTiles);
    }
    
    // 洗牌算法
    private void ShuffleTiles(List<MahjongType> tiles)
    {
        System.Random random = new System.Random();
        int n = tiles.Count;
        
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            MahjongType temp = tiles[k];
            tiles[k] = tiles[n];
            tiles[n] = temp;
        }
    }
    
    // 在4个牌夹上创建麻将牌
    private void CreateTilesOnRacks(List<MahjongType> tiles)
    {
        // 获取麻将桌的变换
        Transform tableTransform = null;
        if (mahjongTable != null)
        {
            tableTransform = mahjongTable.transform;
        }
        else
        {
            Debug.LogError("找不到麻将桌，无法正确定位牌夹");
            return;
        }
        
        // 根据用户手动设置的确切位置
        Vector3[] rackPositions = new Vector3[4]
        {
            new Vector3(-0.0123f, 0.001f, 0.355f),     // 上方（远）
            new Vector3(-0.355f, 0.001f, 0.02f),    // 左方
            new Vector3(0.018f, 0.02f, -0.366f),    // 下方（近）
            new Vector3(0.355f, 0.001f, -0.02f)      // 右方
        };

        // 确切的旋转角度
        Vector3[] rackRotations = new Vector3[4]
        {
            new Vector3(0, 180, 0),  // 上方（远）
            new Vector3(0, 270, 0),  // 左方
            new Vector3(0, 0, 0),    // 下方（近）
            new Vector3(0, 90, 0)    // 右方
        };

        // 创建4个父对象作为牌夹
        GameObject[] racks = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            racks[i] = new GameObject("MahjongRack_" + i);
            
            // 设置牌夹的父对象
            racks[i].transform.SetParent(this.transform);
            
            // 设置确切的位置和旋转
            racks[i].transform.position = rackPositions[i];
            racks[i].transform.rotation = Quaternion.Euler(rackRotations[i]);

            // 如果启用动画，将牌夹移动到起始位置（下方）
            if (enableAnimation)
            {
                Vector3 startPos = rackPositions[i];
                startPos.y -= animationHeight;
                racks[i].transform.position = startPos;
            }
        }

        // 麻将牌尺寸与间隔
        float tileWidth = 0.035f;      // 麻将牌宽度
        float tileDepth = 0.02f;       // 麻将牌厚度
        float tileSpacing = 0.002f;    // 牌间距
        float stackHeight = 0.021f;     // 竖向堆叠高度

        // 每个牌夹的牌计数
        int[] rackTileCount = new int[4] { 0, 0, 0, 0 };
        
        // 标准麻将为136张牌，每个牌夹分配34张
        int totalTilesCreated = 0;
        
        // 为每张牌计算其应该放在哪个牌夹上
        for (int i = 0; i < tiles.Count; i++)
        {
            // 根据牌索引计算放在哪个牌夹上
            int rackIndex = i % 4;
            
            // 计算这个牌夹上的牌数
            int rackTileIndex = rackTileCount[rackIndex];
            
            // 计算行和列
            int row = rackTileIndex / 17; // 每行最多17张牌
            int col = rackTileIndex % 17;
            
            // 确保不超过2行
            if (row >= 2) continue;
            
            // 创建麻将牌
            GameObject tile = Instantiate(mahjongPrefab);
            
            // 设置父物体
            tile.transform.SetParent(racks[rackIndex].transform);
            
            // 计算这一行有多少牌
            int tilesInThisRow = (rackIndex == 3 && row == 1) ? tiles.Count / 4 - 17 : 17;
            
            // 计算整行的宽度（考虑间距）
            float rowWidth = tilesInThisRow * (tileWidth + tileSpacing) - tileSpacing;
            
            // 计算行的起始位置（居中）
            float startX = -rowWidth / 2;
            
            // X坐标（考虑间距）
            float posX = startX + col * (tileWidth + tileSpacing);
            
            // 设置位置
            tile.transform.localPosition = new Vector3(
                posX,                 // X坐标
                stackHeight * row,    // Y坐标（层叠）
                0                     // Z坐标
            );
            
            // 设置旋转和缩放
            tile.transform.localRotation = Quaternion.Euler(0, 180, 0);
            tile.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // 设置名称
            tile.name = "Mahjong_" + rackIndex + "_" + row + "_" + col;
            
            // 为牌设置颜色
            ApplyTileTexture(tile, tiles[i]);
            
            // 记录牌的类型
            mahjongTiles.Add(tile);
            tileTypeMap.Add(tile, tiles[i]);
            
            // 更新计数
            rackTileCount[rackIndex]++;
            totalTilesCreated++;
        }
        
        // 输出每个牌夹的牌数
        for (int i = 0; i < 4; i++)
        {
            Debug.Log("牌夹 " + i + " 创建了 " + rackTileCount[i] + " 张牌");
        }
        
        Debug.Log("总共创建了 " + totalTilesCreated + " 张实体麻将牌");

        // 如果启用动画，使用DOTween执行动画
        if (enableAnimation)
        {
            AnimateRacks(racks, rackPositions);
        }
    }
    
    // 应用麻将牌纹理（这里只是示例，需要根据实际资源进行适配）
    private void ApplyTileTexture(GameObject tile, MahjongType type)
    {
        // TODO: 根据项目中的实际资源和需求设置麻将牌的外观
        // 例如，可以通过更改材质、添加特定的纹理等方式来区分不同类型的麻将牌
        
        // 这里只是一个简单的演示，实际实现可能需要更复杂的逻辑
        Renderer renderer = tile.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            // 这里可以根据type设置不同的材质或颜色
            // 示例：根据牌的类型设置不同的颜色
            Color tileColor = Color.white;
            
            // 根据牌的类型设置颜色（仅用于演示）
            // if (type.ToString().StartsWith("Dot"))
            // {
            //     tileColor = new Color(1.0f, 0.8f, 0.8f); // 浅红色for筒子
            // }
            // else if (type.ToString().StartsWith("Bamboo"))
            // {
            //     tileColor = new Color(0.8f, 1.0f, 0.8f); // 浅绿色for条子
            // }
            // else if (type.ToString().StartsWith("Character"))
            // {
            //     tileColor = new Color(0.8f, 0.8f, 1.0f); // 浅蓝色for万子
            // }
            
            // 创建新材质以避免修改原始材质
            Material newMaterial = new Material(renderer.material);
            newMaterial.color = tileColor;
            renderer.material = newMaterial;
        }
    }
    
    // 清除所有麻将牌
    public void ClearMahjongTiles()
    {
        // 销毁所有麻将牌对象
        foreach (GameObject tile in mahjongTiles)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
        
        // 清空列表和映射
        mahjongTiles.Clear();
        tileTypeMap.Clear();
        
        // 删除牌夹对象
        for (int i = 0; i < 4; i++)
        {
            GameObject rack = GameObject.Find("MahjongRack_" + i);
            if (rack != null)
            {
                Destroy(rack);
            }
        }
    }
    
    // 获取麻将牌类型
    public MahjongType GetTileType(GameObject tile)
    {
        if (tileTypeMap.ContainsKey(tile))
        {
            return tileTypeMap[tile];
        }
        
        // 如果找不到，返回默认值
        Debug.LogWarning("找不到该麻将牌的类型");
        return MahjongType.Dot1;
    }

    // 使用DOTween实现动画
    private void AnimateRacks(GameObject[] racks, Vector3[] targetPositions)
    {
        for (int i = 0; i < racks.Length; i++)
        {
            racks[i].transform
                .DOMove(targetPositions[i], animationDuration)
                .SetEase(animationEase)
                .SetDelay(1f); // 添加一个小的延迟，使动画更有层次感
        }
    }
} 