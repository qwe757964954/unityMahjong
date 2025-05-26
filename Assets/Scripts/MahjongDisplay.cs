using UnityEngine;

public class MahjongDisplay : MonoBehaviour
{
    public MahjongManager.MahjongType type;
    public Mesh[] meshes; // 按照 MahjongType 枚举顺序排列
    public MeshFilter meshFilter;

    void Start()
    {
        UpdateMesh();
    }

    public void SetType(MahjongManager.MahjongType newType)
    {
        type = newType;
        UpdateMesh();
    }

    void UpdateMesh()
    {
        if (meshes != null && meshFilter != null && (int)type < meshes.Length)
        {
            meshFilter.mesh = meshes[(int)type];
        }
    }
}
