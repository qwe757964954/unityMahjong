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

        [SerializeField]
        private TileAnimator tileAnimator; // Add reference to TileAnimator

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

                int discardIndex = discardAnchor.childCount-1;
                TilePositioner.PositionDiscardTile(tile.GameObject, discardAnchor, discardIndex);

                if (tileAnimator != null)
                {
                    // await tileAnimator.AnimateDiscardAsync(tile, tile.GameObject.transform.localPosition, cancellationToken);
                }

                Debug.Log($"Discarded tile: {tile.Suit} {tile.Number}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discard tile {tile.Suit} {tile.Number}: {ex.Message}");
                return false;
            }
        }
    }
}