using UnityEngine;
using UnityEngine.Playables;

namespace MahjongGame
{
    public class MahjongTableTimeline : MonoBehaviour
    {
        public PlayableDirector playableDirector;
        
        private void Start()
        {
            // 确保PlayableDirector组件已正确赋值
            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }
        }
        
        public void PlayTimeline()
        {
            if (playableDirector != null)
            {
                playableDirector.time = 0; // ✅ 回到起始时间
                playableDirector.Evaluate(); // ✅ 立即刷新状态
                playableDirector.Play();
            }
            else
            {
                Debug.LogWarning("PlayableDirector is not assigned.");
            }
        }
        public void ResetAndPlayTimeline()
        {
            if (playableDirector != null)
            {
                playableDirector.Stop();     // 停止当前播放（如果正在播放）
                playableDirector.time = 0;   // 重置时间
                playableDirector.Evaluate(); // 应用初始状态
                playableDirector.Play();     // 开始播放
            }
        }

    }
}