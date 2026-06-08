namespace Nacos.V2.Security
{
    using Microsoft.Extensions.Logging;
    using Nacos.V2.Utils;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class SecurityProxy : ISecurityProxy
    {
        private static readonly string LOGIN_URL_V1 = "/v1/auth/users/login";
        private static readonly string LOGIN_URL_V3 = "/v3/auth/user/login";
        private static readonly int Unit = 1000;
        private static readonly int LoginTimeOut = 5000;

        private readonly HttpClient _httpClient;

        private string contextPath;

        /// <summary>
        /// User's name
        /// </summary>
        private string _username;

        /// <summary>
        /// User's password
        /// </summary>
        private string _password;

        /// <summary>
        /// A token to take with when sending request to Nacos server
        /// </summary>
        private string _accessToken;

        /// <summary>
        /// TTL of token in seconds
        /// </summary>
        private long _tokenTtl;

        /// <summary>
        /// Last timestamp refresh security info from server
        /// </summary>
        private long _lastRefreshTime;

        /// <summary>
        /// time window to refresh security info in seconds
        /// </summary>
        private long _tokenRefreshWindow;

        /// <summary>
        /// null = not yet determined; true = use v3; false = v3 not available, use v1
        /// </summary>
        private bool? _useV3Login;

        private readonly NacosSdkOptions _options;

        private readonly ILogger _logger;

        public SecurityProxy(NacosSdkOptions options, ILogger logger)
        {
            _options = options;

            _username = _options.UserName ?? "";
            _password = _options.Password ?? "";
            contextPath = _options.ContextPath;
            contextPath = contextPath.StartsWith("/") ? contextPath : "/" + contextPath;

            _logger = logger;
            _httpClient = new HttpClient();
        }

        // for test
        internal SecurityProxy(NacosSdkOptions options, ILogger logger, HttpMessageHandler httpMessageHandler)
        {
            _options = options;

            _username = _options.UserName ?? "";
            _password = _options.Password ?? "";
            contextPath = _options.ContextPath;
            contextPath = contextPath.StartsWith("/") ? contextPath : "/" + contextPath;

            _logger = logger;
            _httpClient = new HttpClient(httpMessageHandler);
        }

        public async Task<bool> LoginAsync(List<string> servers)
        {
            try
            {
                if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - _lastRefreshTime) < (_tokenTtl - _tokenRefreshWindow) * Unit)
                {
                    return true;
                }

                foreach (var server in servers)
                {
                    var flag = await LoginAsync(server.TrimEnd('/')).ConfigureAwait(false);
                    if (flag)
                    {
                        _lastRefreshTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public string GetAccessToken()
        {
            return _accessToken;
        }

        public bool IsEnabled() => this._username.IsNotNullOrWhiteSpace();

        internal async Task<bool> LoginAsync(string server)
        {
            if (_username.IsNotNullOrWhiteSpace())
            {
                var baseUrl = server.Contains(Nacos.V2.Common.Constants.HTTP_PREFIX)
                    ? $"{server}{contextPath}"
                    : $"{Naming.Utils.UtilAndComs.HTTP}{server}{contextPath}";

                var dict = new Dictionary<string, string>
                {
                    { Common.PropertyKeyConst.USERNAME, _username },
                    { Common.PropertyKeyConst.PASSWORD, _password }
                };

                if (_useV3Login != false)
                {
                    var v3Result = await TryLoginAsync(
                        $"{baseUrl}{LOGIN_URL_V3}",
                        () => new FormUrlEncodedContent(dict)
                    ).ConfigureAwait(false);

                    if (v3Result.RememberV3) _useV3Login = true;
                    if (v3Result.Success) return true;
                    if (!v3Result.ShouldFallbackToV1) return false;

                    _useV3Login = false;
                }

                var v1Result = await TryLoginAsync(
                    $"{baseUrl}{LOGIN_URL_V1}",
                    () => new FormUrlEncodedContent(dict)
                ).ConfigureAwait(false);

                return v1Result.Success;
            }

            return true;
        }

        private async Task<(bool Success, bool ShouldFallbackToV1, bool RememberV3)> TryLoginAsync(
            string url, Func<HttpContent> contentFactory)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(LoginTimeOut));

                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = contentFactory()
                };

                using var resp = await _httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);
                var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                var shouldFallbackToV1 = resp.StatusCode == System.Net.HttpStatusCode.NotFound
                    || resp.StatusCode == System.Net.HttpStatusCode.NotImplemented
                    || (int)resp.StatusCode >= 500;

                if (!resp.IsSuccessStatusCode)
                {
                    _logger?.LogError("login failed: {0}", content);
                    return (false, shouldFallbackToV1, !shouldFallbackToV1);
                }

                var obj = System.Text.Json.Nodes.JsonNode.Parse(content).AsObject();

                if (obj.ContainsKey(Nacos.V2.Common.Constants.ACCESS_TOKEN))
                {
                    _accessToken = obj[Nacos.V2.Common.Constants.ACCESS_TOKEN].GetValue<string>();
                    _tokenTtl = obj[Nacos.V2.Common.Constants.TOKEN_TTL].GetValue<long>();
                    _tokenRefreshWindow = _tokenTtl / 10;
                }

                return (true, false, true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[SecurityProxy] login http request failed, url: {0}", url);
                return (false, false, false);
            }
        }
    }
}
