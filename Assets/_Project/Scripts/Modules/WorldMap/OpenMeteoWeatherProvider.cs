#nullable enable
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// Open-Meteo 当前天气适配器。
    /// Open-Meteo 不需要 API key；坐标和时区由 Scene/Inspector 传入。
    /// </summary>
    public sealed class OpenMeteoWeatherProvider : IWeatherProvider
    {
        private const string Endpoint = "https://api.open-meteo.com/v1/forecast";

        public Task<WeatherSnapshot> FetchCurrentAsync(
            double latitude,
            double longitude,
            string timezone,
            CancellationToken cancellationToken)
        {
            if (latitude is < -90d or > 90d)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), "纬度必须在 -90 到 90 之间。");
            }

            if (longitude is < -180d or > 180d)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), "经度必须在 -180 到 180 之间。");
            }

            string safeTimezone = string.IsNullOrWhiteSpace(timezone) ? "auto" : timezone.Trim();
            string url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?latitude={1}&longitude={2}&current=weather_code&timezone={3}",
                Endpoint,
                latitude,
                longitude,
                Uri.EscapeDataString(safeTimezone));

            var completion = new TaskCompletionSource<WeatherSnapshot>();
            if (cancellationToken.IsCancellationRequested)
            {
                completion.SetCanceled();
                return completion.Task;
            }

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            CancellationTokenRegistration registration = default;
            registration = cancellationToken.Register(() =>
            {
                if (!operation.isDone)
                {
                    request.Abort();
                }
            });

            operation.completed += _ =>
            {
                registration.Dispose();
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled();
                    }
                    else if (request.result != UnityWebRequest.Result.Success)
                    {
                        completion.TrySetException(new InvalidOperationException(
                            $"Open-Meteo 请求失败（{request.responseCode}）：{request.error}"));
                    }
                    else
                    {
                        completion.TrySetResult(ParseSnapshot(request.downloadHandler.text, DateTime.UtcNow));
                    }
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
                finally
                {
                    request.Dispose();
                }
            };

            return completion.Task;
        }

        /// <summary>公开解析入口，供 EditMode 测试和未来其他天气传输层复用。</summary>
        public static WeatherSnapshot ParseSnapshot(string json, DateTime fallbackObservedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("天气响应为空。");
            }

            OpenMeteoResponse? response = JsonUtility.FromJson<OpenMeteoResponse>(json);
            if (response?.current is null)
            {
                throw new InvalidOperationException("天气响应缺少 current 节点。");
            }

            DateTime observedAtUtc = fallbackObservedAtUtc.Kind == DateTimeKind.Utc
                ? fallbackObservedAtUtc
                : fallbackObservedAtUtc.ToUniversalTime();
            if (DateTime.TryParse(
                    response.current.time,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                observedAtUtc = parsed;
            }

            int code = response.current.weather_code;
            return new WeatherSnapshot(WorldMapWeatherClassifier.Classify(code), code, observedAtUtc);
        }

        [Serializable]
        private sealed class OpenMeteoResponse
        {
            public OpenMeteoCurrent? current;
        }

        [Serializable]
        private sealed class OpenMeteoCurrent
        {
            public string time = string.Empty;
            public int weather_code;
        }
    }
}
