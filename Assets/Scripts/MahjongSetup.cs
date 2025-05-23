using UnityEngine;

public class MahjongSetup : MonoBehaviour
{
    // 麻将管理器
    private MahjongManager mahjongManager;

    // 麻将牌预制体引用
    public GameObject mahjongPrefab;
    
    // 麻将桌引用
    public GameObject mahjongTable;

    void Start()
    {
        // 创建麻将管理器
        SetupMahjongManager();
    }

    // 设置麻将管理器
    private void SetupMahjongManager()
    {
        // 获取或创建麻将管理器
        mahjongManager = GetComponent<MahjongManager>();
        if (mahjongManager == null)
        {
            mahjongManager = gameObject.AddComponent<MahjongManager>();
        }

        // 查找麻将桌
        if (mahjongTable == null)
        {
            mahjongTable = GameObject.Find("Mahjong table_009 1");
            if (mahjongTable == null)
            {
                Debug.LogWarning("未找到麻将桌对象，麻将牌将使用默认位置");
            }
        }
        
        // 设置麻将桌引用
        mahjongManager.mahjongTable = mahjongTable;

        // 设置麻将牌预制体
        if (mahjongPrefab != null)
        {
            mahjongManager.mahjongPrefab = mahjongPrefab;
        }
        else
        {
            // 尝试在项目中查找麻将牌预制体
            mahjongPrefab = Resources.Load<GameObject>("Mahjong");
            if (mahjongPrefab == null)
            {
                Debug.LogWarning("未找到麻将牌预制体，请手动设置mahjongPrefab");
                
                // 查找项目中的预制体
                mahjongPrefab = GameObject.Find("Mahjong")?.gameObject;
                if (mahjongPrefab == null)
                {
                    Debug.LogError("无法找到麻将牌预制体，请确保已添加到场景或设置预制体引用");
                    return;
                }
            }
            mahjongManager.mahjongPrefab = mahjongPrefab;
        }

        // 初始化麻将牌
        mahjongManager.InitializeMahjongTiles();
    }

    // 重新洗牌并布置麻将牌
    public void ResetMahjong()
    {
        if (mahjongManager != null)
        {
            mahjongManager.InitializeMahjongTiles();
        }
    }
} 