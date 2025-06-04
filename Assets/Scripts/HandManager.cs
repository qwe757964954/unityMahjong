using UnityEngine; using Cysharp.Threading.Tasks; using System; using System.Threading.Tasks; using System.Linq; using System.Collections.Generic; using DG.Tweening; using System.Threading;

namespace MahjongGame
{
    public class HandManager : MonoBehaviour
    {
        [Header("HandSelfPlaying")] [SerializeField]
        private Transform HandSelfPlaying;

        [Header("Anchor Transforms (Down, Left, Up, Right)")] [SerializeField]
        private Transform[] anchorTransforms = new Transform[4];

        private RackManager rackManager;
        private TileDealer tileDealer;

        public void Initialize(GameObject table, RackManager rack)
        {
            rackManager = rack;
            tileDealer = new TileDealer(rackManager);
            if (rackManager == null)
            {
                Debug.LogError("Required dependencies not assigned in HandManager. Disabling component.");
                enabled = false;
            }
        }

        public void DrawTileAsync(int playerIndex, MahjongTile tile)
        {
            Transform handAnchor = GetHandAnchor(playerIndex, true);
            if (handAnchor == null) return;

            int handIndex = handAnchor.childCount;
            int totalCards = handIndex + 1;

            GameObject tileObj = tile.GameObject;
            tileObj.transform.SetParent(handAnchor, false);
            tileObj.transform.localScale = Vector3.one;
            bool isSelfPlayer = (playerIndex == 0 && handAnchor == HandSelfPlaying);
            int layer = isSelfPlayer ? LayerMask.NameToLayer("PlayerHandLayer") : LayerMask.NameToLayer("Default");
            LayerUtil.SetLayerRecursively(tileObj, layer);
            TilePositioner.DrawPositionTile(tileObj, handAnchor, totalCards, isSelfPlayer);
        }

        public async UniTask<bool> DealHandCardsByDiceAsync(CancellationToken cancellationToken)
        {
            int banker = GameDataManager.Instance.BankerIndex;
            PlayerHandState[] handStates = InitializeHandStates(banker);

            // 每轮发4家，每家4张
            for (int round = 0; round < 3; round++)
            {
                List<Transform> roundNewTiles = new List<Transform>();
                for (int p = 0; p < 4; p++)
                {
                    int player = (banker + p) % 4;
                    var newTiles = tileDealer.DealTiles(handStates[player], 4, cancellationToken);
                    roundNewTiles.AddRange(newTiles);
                }

                await tileDealer.FlipTilesAsync(roundNewTiles);
            }

            // 最后两轮，先每家1张，再庄家补1张
            {
                List<Transform> roundNewTiles = new List<Transform>();
                for (int p = 0; p < 4; p++)
                {
                    int player = (banker + p) % 4;
                    var newTiles = tileDealer.DealTiles(handStates[player], 1, cancellationToken);
                    roundNewTiles.AddRange(newTiles);
                }

                await tileDealer.FlipTilesAsync(roundNewTiles);
            }
            {
                int player = banker;
                // 庄家补的最后一张牌，添加半张牌的间隔
                var newTiles = tileDealer.DealTiles(handStates[player], 1, cancellationToken, isBankerExtraTile: true);
                await tileDealer.FlipTilesAsync(newTiles);
            }

            await UniTask.WhenAll(Enumerable.Range(0, 4).Select(p => RevealHandAsync(handStates[p].Anchor)));

            return true;
        }

        private PlayerHandState[] InitializeHandStates(int banker)
        {
            PlayerHandState[] handStates = new PlayerHandState[4];
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = (i == 0) ? HandSelfPlaying : anchorTransforms[i];
                int totalCards = (i == banker) ? MahjongConfig.EastExtraCard : MahjongConfig.InitialHandCount;
                handStates[i] = new PlayerHandState(anchor, totalCards);
            }

            return handStates;
        }

        private async UniTask RevealHandAsync(Transform anchor)
        {
            var resetTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                resetTasks.Add(tile.DOLocalRotate(Vector3.zero, 0.1f).ToUniTask());
            }

            await UniTask.WhenAll(resetTasks);
            await UniTask.Delay(100);

            var flipTasks = new List<UniTask>();
            foreach (Transform tile in anchor)
            {
                flipTasks.Add(tile.DOLocalRotate(new Vector3(-90, 0, 0), 0.25f).ToUniTask());
            }

            await UniTask.WhenAll(flipTasks);
        }

        public void RefreshHandPositions(int playerIndex, bool isReveal)
        {
            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);
            if (handAnchor == null || handAnchor.childCount == 0)
            {
                Debug.LogWarning($"No tiles to refresh for player {playerIndex}.");
                return;
            }

            bool isSelfPlayer = (playerIndex == 0 && handAnchor == HandSelfPlaying);
            int totalCards = handAnchor.childCount;

            for (int i = 0; i < handAnchor.childCount; i++)
            {
                Transform tileTransform = handAnchor.GetChild(i);
                GameObject tileObj = tileTransform.gameObject;

                int layer = isSelfPlayer ? LayerMask.NameToLayer("PlayerHandLayer") : LayerMask.NameToLayer("Default");
                LayerUtil.SetLayerRecursively(tileObj, layer);

                TilePositioner.PositionTile(tileObj, handAnchor, i, totalCards);

                tileTransform.localRotation = Quaternion.Euler(-90, 0, 0);
            }
        }

        public MahjongTile GetHandTile(int playerIndex, bool isReveal, int indexFromEnd = 0)
        {
            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);
            if (handAnchor == null || handAnchor.childCount == 0) return null;

            int targetIndex = Mathf.Clamp(handAnchor.childCount - 1 - indexFromEnd, 0, handAnchor.childCount - 1);
            Transform tileTransform = handAnchor.GetChild(targetIndex);
            MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
            return display?.TileData;
        }

        public List<MahjongTile> GetLastNHandTiles(int playerIndex, bool isReveal, int count)
        {
            List<MahjongTile> result = new List<MahjongTile>();
            Transform handAnchor = GetHandAnchor(playerIndex, isReveal);

            if (handAnchor == null || handAnchor.childCount == 0)
            {
                Debug.LogWarning($"No tiles in hand for player {playerIndex}.");
                return result;
            }

            int startIndex = Mathf.Max(handAnchor.childCount - count, 0);

            for (int i = startIndex; i < handAnchor.childCount; i++)
            {
                Transform tileTransform = handAnchor.GetChild(i);
                MahjongDisplay display = tileTransform.GetComponent<MahjongDisplay>();
                if (display != null && display.TileData != null)
                {
                    result.Add(display.TileData);
                }
                else
                {
                    Debug.LogWarning($"Missing MahjongDisplay or TileData on hand tile at index {i}.");
                }
            }

            return result;
        }

        public Transform GetHandAnchor(int playerIndex, bool isReveal)
        {
            if (playerIndex == 0 && isReveal)
            {
                return HandSelfPlaying;
            }

            if (playerIndex < 0 || playerIndex >= anchorTransforms.Length)
            {
                Debug.LogError($"Invalid playerIndex: {playerIndex}");
                return null;
            }

            Transform anchor = anchorTransforms[playerIndex];
            return anchor;
        }
    }

    public class PlayerHandState
    {
        public Transform Anchor { get; private set; }
        public int TotalCards { get; private set; }
        public int CurrentCardCount { get; set; }

        public PlayerHandState(Transform anchor, int totalCards)
        {
            Anchor = anchor;
            TotalCards = totalCards;
            CurrentCardCount = 0;
        }
    }
}
