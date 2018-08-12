using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Robinhood.Domain.Internal;
using Robinhood.Uri;

namespace Robinhood.Client
{
    public partial class RobinhoodClient
    {
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };


        public static async Task<RobinhoodClient> GetClient(string username, string password)
        {
            var client = new RobinhoodClient();
            Init();
            await Login(username, password);
            return client;
        }

        private static void Init()
        {
            Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Http.DefaultRequestHeaders.Add("User-Agent", "Robinhood/2357 (Android/2.19.0)");
        }

        private static async Task Login(string username, string password)
        {
            var uri = new UriBuilder
            {
                Scheme = "https",
                Host = ApiAddress.RootUri,
                Path = ApiAddress.Login
            }.Uri;
            var postBody = new
            {
                username,
                password
            };
            var request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
            };
            var response = await MakeRequest(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(body);
            if (tokenResponse.MfaRequired)
            {
                throw new NotSupportedException("MFA login is not supported at this time.");
            }

            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", tokenResponse.Token);
        }

        private static async Task<HttpResponseMessage> MakeRequest(HttpRequestMessage request)
        {
            while (true)
            {
                var response = await Http.SendAsync(request);
                var statusCode = (int) response.StatusCode;

                // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
                if (statusCode < 300 || statusCode > 399)
                {
                    return response.IsSuccessStatusCode
                        ? response
                        : throw new HttpRequestException(await response.Content.ReadAsStringAsync());
                }

                var redirectUri = response.Headers.Location;
                if (!redirectUri.IsAbsoluteUri)
                {
                    redirectUri = new System.Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                }

                var newRequest = new HttpRequestMessage
                {
                    Content = request.Content,
                    RequestUri = redirectUri,
                    Method = request.Method,
                    Version = request.Version
                };
                request = newRequest;
            }
        }

        private RobinhoodClient()
        {
        }
    }
}