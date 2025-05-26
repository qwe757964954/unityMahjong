using UnityEngine;
using DG.Tweening;
using MahjongGame;

namespace MahjongGame
{
    public class MahjongDisplay : MonoBehaviour
    {
        public MahjongType Type { get; private set; }
        public MeshFilter MeshFilter;
        private Mesh[] Meshes;

        private void Awake()
        {
            LoadMeshes();
        }

        private void LoadMeshes()
        {
            Mesh[] allMeshes = Resources.LoadAll<Mesh>("Meshes");
            if (allMeshes == null || allMeshes.Length == 0)
            {
                Debug.LogError("No Mesh resources found in Resources/Meshes folder!");
                return;
            }

            var mahjongTypes = System.Enum.GetValues(typeof(MahjongType));
            Meshes = new Mesh[mahjongTypes.Length];

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
                            Meshes[(int)type] = mesh;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < Meshes.Length; i++)
            {
                if (Meshes[i] == null)
                {
                    Debug.LogWarning($"Mesh not found for MahjongType: {(MahjongType)i}, expected pinyin: {MahjongTileData.GetPinyinForMahjongType((MahjongType)i)}");
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
            if (Meshes != null && MeshFilter != null && (int)Type < Meshes.Length && Meshes[(int)Type] != null)
            {
                MeshFilter.mesh = Meshes[(int)Type];
            }
            else
            {
                Debug.LogWarning($"Mesh not loaded for Type: {Type}");
            }
        }

        public void PlayDrawAnimation(Vector3 targetPosition)
        {
            transform.DOMove(targetPosition, 0.5f).SetEase(Ease.OutQuad);
        }
    }
}