// Visual representation of a Mahjong tile
using UnityEngine;
using DG.Tweening;
namespace MahjongGame
{
    public class MahjongDisplay : MonoBehaviour
    {
        public MahjongType Type { get; private set; }
        [SerializeField] private MeshFilter meshFilter;
        public MahjongTile TileData { get; private set; }  // ✅ 新增：持有数据引用
        private static Mesh[] meshes; // Static cache for meshes

        private void Awake()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            LoadMeshes();
        }
        public void BindTile(MahjongTile tile)
        {
            TileData = tile;
            SetType(tile.Type); // 可选，自动设置类型
        }
        private static void LoadMeshes()
        {
            Mesh[] allMeshes = Resources.LoadAll<Mesh>("Meshes");
            var mahjongTypes = System.Enum.GetValues(typeof(MahjongType));
            meshes = new Mesh[mahjongTypes.Length];

            foreach (Mesh mesh in allMeshes)
            {
                string meshName = mesh.name;
                if (meshName.StartsWith("Mahjong_"))
                {
                    string pinyinName = meshName.Substring("Mahjong_".Length);
                    foreach (MahjongType type in mahjongTypes)
                    {
                        string expectedPinyin = MahjongTileData.GetPinyinForMahjongType(type);
                        if (pinyinName == expectedPinyin)
                        {
                            meshes[(int)type] = mesh;
                            break;
                        }
                    }
                }
            }
        }

        public void SetType(MahjongType newType)
        {
            Type = newType;
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (meshes != null && meshFilter != null && (int)Type < meshes.Length && meshes[(int)Type] != null)
            {
                meshFilter.mesh = meshes[(int)Type];
            }
            else
            {
                Debug.LogWarning($"Mesh not loaded for Type: {Type} on {gameObject.name}");
            }
        }

        public void PlayDrawAnimation(Vector3 targetPosition)
        {
            transform.DOMove(targetPosition, MahjongConfig.AnimationDuration).SetEase(Ease.OutQuad);
        }
    }
}