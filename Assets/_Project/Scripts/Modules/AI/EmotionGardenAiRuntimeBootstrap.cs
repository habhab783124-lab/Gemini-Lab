#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.AI
{
    /// <summary>注册情绪花园 AI 提供器；不创建或修改任何场景视觉对象。</summary>
    public static class EmotionGardenAiRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (ServiceLocator.TryResolve<IEmotionGardenAiProvider>(out _))
            {
                return;
            }

            LLMConfigSO? config = Resources.Load<LLMConfigSO>("LLMConfig");
            if (config == null)
            {
                Debug.LogWarning("[EmotionGardenAI] Resources/LLMConfig 不存在，情绪花园使用本地兜底。");
                return;
            }

            ServiceLocator.Register<IEmotionGardenAiProvider>(new EmotionGardenAiProvider(config));
            Debug.Log($"[EmotionGardenAI] 提供器已注册（configured={config.IsConfigured}）。");
        }
    }
}
