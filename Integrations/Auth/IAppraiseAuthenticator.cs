using Integrations.Configuration;
using Integrations.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Integrations.Auth
{
    public interface IIAppraiseAuthenticator
    {
        Task<string> GetTokenAsync(CancellationToken ct = default);
        void Invalidate();
    }

    public class IAppraiseAuthenticator : IIAppraiseAuthenticator
    {
        public const string HttpClientName = "IAppraiseAuth";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAppraiseOptions _options;
        private readonly ILogger<IAppraiseAuthenticator> _logger;

        private readonly SemaphoreSlim _lock = new(1, 1);
        private string? _cachedToken;

        public IAppraiseAuthenticator(
            IHttpClientFactory httpClientFactory,
            IOptions<IAppraiseOptions> options,
            ILogger<IAppraiseAuthenticator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(_options.StaticToken))
            {
                return _options.StaticToken;
            }

            if (_cachedToken is not null)
            {
                return _cachedToken;
            }

            await _lock.WaitAsync(ct);
            try
            {
                if (_cachedToken is not null)
                {
                    return _cachedToken;
                }

                _cachedToken = await LoginAsync(ct);
                return _cachedToken;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Invalidate()
        {
            _cachedToken = null;
        }

        private async Task<string> LoginAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
            {
                throw new InvalidOperationException(
                    "iAppraise credentials are not configured. Set IAppraise:Username and IAppraise:Password, or provide IAppraise:StaticToken.");
            }

            var http = _httpClientFactory.CreateClient(HttpClientName);

            _logger.LogInformation("Logging in to iAppraise as {Username}", _options.Username);

            var resp = await http.PostAsJsonAsync(
                "login/",
                new { username = _options.Username, password = _options.Password },
                ct);

            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(body?.Key))
            {
                throw new InvalidOperationException("iAppraise login did not return a token key.");
            }

            return body.Key;
        }
    }
}
