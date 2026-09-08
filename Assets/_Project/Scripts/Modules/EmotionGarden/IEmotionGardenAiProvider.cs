#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.EmotionGarden
{
    /// <summary>
    /// 情绪花园 AI 内容提供器。实现可以来自真实 LLM，也可以是测试替身；
    /// 情绪花园业务只依赖这个接口，不直接依赖网络或 UI。
    /// </summary>
    public interface IEmotionGardenAiProvider
    {
        Task<EmotionGardenAiResult?> GenerateAsync(
            string dateIso,
            string inputSentence,
            string emotionType,
            string owner,
            string flowerName,
            string flowerDescription,
            CancellationToken cancellationToken = default);
    }
}
