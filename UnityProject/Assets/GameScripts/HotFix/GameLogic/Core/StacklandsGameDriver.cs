using UnityEngine;

namespace GameLogic.Core
{
    /// <summary>
    /// 将 Unity 帧时间转换为 Core 内部固定步长，玩法规则不依赖 MonoBehaviour。
    /// </summary>
    public sealed class StacklandsGameDriver : MonoBehaviour
    {
        private const float FixedStep = 1f / 30f;
        private float _accumulator;

        private void Update()
        {
            _accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            while (_accumulator >= FixedStep)
            {
                CoreSystem.Tick(FixedStep);
                _accumulator -= FixedStep;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                CoreSystem.Save();
        }

        private void OnApplicationQuit()
        {
            CoreSystem.Save();
        }
    }
}
