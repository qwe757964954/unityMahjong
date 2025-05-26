using UnityEngine;
using MahjongGame;

public class MahjongSetup : MonoBehaviour
{
    private MahjongManager mahjongManager;
    public GameObject mahjongPrefab;
    public GameObject mahjongTable;

    void Start()
    {
        SetupMahjongManager();
    }

    private void SetupMahjongManager()
    {
        mahjongManager = GetComponent<MahjongManager>();
        if (mahjongManager == null)
        {
            mahjongManager = gameObject.AddComponent<MahjongManager>();
        }

        if (mahjongTable == null)
        {
            mahjongTable = GameObject.Find("Mahjong table_009 1");
            if (mahjongTable == null)
            {
                Debug.LogWarning("未找到麻将桌对象，麻将牌将使用默认位置");
            }
        }
        mahjongManager.MahjongTable = mahjongTable;

        if (mahjongPrefab != null)
        {
            mahjongManager.MahjongPrefab = mahjongPrefab;
        }
        else
        {
            mahjongPrefab = Resources.Load<GameObject>("Mahjong");
            if (mahjongPrefab == null)
            {
                Debug.LogWarning("未找到麻将牌预制体，请手动设置mahjongPrefab");
                mahjongPrefab = GameObject.Find("Mahjong")?.gameObject;
                if (mahjongPrefab == null)
                {
                    Debug.LogError("无法找到麻将牌预制体，请确保已添加到场景或设置预制体引用");
                    return;
                }
            }
            mahjongManager.MahjongPrefab = mahjongPrefab;
        }

        mahjongManager.InitializeMahjongTiles();
    }

    public void ResetMahjong()
    {
        if (mahjongManager != null)
        {
            mahjongManager.InitializeMahjongTiles();
        }
    }
}