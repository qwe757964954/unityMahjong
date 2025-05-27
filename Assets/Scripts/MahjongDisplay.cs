// Visual representation of a Mahjong tile
using UnityEngine;
using DG.Tweening;
namespace MahjongGame
{
    public class MahjongDisplay : MonoBehaviour
    {
        public MahjongType Type { get; private set; }
        [SerializeField] private MeshFilter meshFilter;
        private static Mesh[] meshes; // Static cache for meshes

        private void Awake()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError($"MeshFilter not found on {gameObject.name}. Disabling component.");
                    enabled = false;
                    return;
                }
            }

            LoadMeshes();
        }

        private static void LoadMeshes()
        {
            if (meshes != null) return; // Already loaded

            Mesh[] allMeshes = Resources.LoadAll<Mesh>("Meshes");
            if (allMeshes == null || allMeshes.Length == 0)
            {
                Debug.LogError("No Mesh resources found in Resources/Meshes folder!");
                return;
            }

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

            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null)
                {
                    Debug.LogWarning(
                        $"Mesh not found for MahjongType: {(MahjongType)i}, expected pinyin: {MahjongTileData.GetPinyinForMahjongType((MahjongType)i)}");
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