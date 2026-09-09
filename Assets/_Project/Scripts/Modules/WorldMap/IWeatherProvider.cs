#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>外部天气数据源的边界；UI 和场景控制器不直接依赖网络协议。</summary>
    public interface IWeatherProvider
    {
        Task<WeatherSnapshot> FetchCurrentAsync(
            double latitude,
            double longitude,
            string timezone,
            CancellationToken cancellationToken);
    }
}
