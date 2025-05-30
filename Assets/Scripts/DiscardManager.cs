using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MahjongGame
{
    public class DiscardManager : MonoBehaviour
    {
        [Header("Anchor Transforms (Down, Left, Up, Right)")]
        [SerializeField]
        public Transform[] anchorTransforms = new Transform[4];
        private void Start()
        {
            // Validate anchorTransforms
            for (int i = 0; i < anchorTransforms.Length; i++)
            {
                if (anchorTransforms[i] == null)
                {
                    Debug.LogWarning($"Discard anchor for player {i} is not assigned.");
                }
            }
        }

        public async UniTask<bool> DiscardTileAsync(MahjongTile tile, int playerIndex, CancellationToken cancellationToken = default)
        {
            // Get the discard anchor from DiscardManager
            if (playerIndex >= anchorTransforms.Length)
            {
                Debug.LogWarning("DiscardManager or its anchorTransforms are not properly set.");
                return false;
            }
            Transform discardAnchor = anchorTransforms[playerIndex];
            if (discardAnchor == null)
            {
                Debug.LogWarning($"Discard anchor for player {playerIndex} is null.");
                return false;
            }
            try
            {
                tile.SetParent(discardAnchor);
                LayerUtil.SetLayerRecursively(tile.GameObject, LayerMask.NameToLayer("Default"));
                int discardIndex = discardAnchor.childCount-1;
                TilePositioner.PositionDiscardTile(tile.GameObject, discardAnchor, discardIndex);

                // if (tileAnimator != null)
                // {
                    // await tileAnimator.AnimateDiscardAsync(tile, tile.GameObject.transform.localPosition, cancellationToken);
                // }

                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discard tile {tile.Suit} {tile.Number}: {ex.Message}");
                return false;
            }
        }
        public MahjongTile GetDiscardTile(int playerIndex, int indexFromEnd = 0)
        {
            Transform discardAnchor = anchorTransforms[playerIndex];
            if (discardAnchor == null || discardAnchor.childCount == 0) return null;
            int targetIndex = Mathf.Clamp(discardAnchor.childCount - 1 - indexFromEnd, 0, discardAnchor.childCount - 1);
            Transform tileTransform = discardAnchor.GetChild(targetIndex);
            MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
            return display?.TileData;
        }
    }
}