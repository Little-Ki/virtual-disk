using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace VirtualDisk.Utils
{

    class HttpResponse(HttpResponseMessage message)
    {
        private readonly HttpResponseMessage message = message;

        private JsonNode? json = null;
        public byte[] Data
        {
            get
            {
                return message.Content.ReadAsByteArrayAsync().Result;
            }
        }

        public string Text {
            get
            {
                return message.Content.ReadAsStringAsync().Result;
            }
        }
        public HttpStatusCode Status
        {
            get
            {
                return message.StatusCode;
            }
        }
        public bool Success
        {
            get
            {
                return Status == HttpStatusCode.OK || Status == HttpStatusCode.PartialContent;
            }
        }

        public Dictionary<string, IEnumerable<string>> Headers() => message.Headers.ToDictionary();

        public T? Json<T>() => JsonConvert.DeserializeObject<T>(Text);

        public JsonNode? Json(params dynamic[] path)
        {
            if (json == null)
            {
                try
                {
                    json = JsonNode.Parse(Text);
                } catch
                {
                    return null;
                }
            }

            var node = json;

            foreach(var it in path)
            {
                if (node == null)
                {
                    return null;
                } else
                {
                    node = node[it];
                }
            }

            return node;
        }

    };

    class Http : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly HttpClientHandler clientHandler;
        private readonly CookieContainer cookieContainer;

        private async Task<HttpResponse> RequestAsync(HttpRequestMessage message)
        {
            try
            {
                var response = await httpClient.SendAsync(message);

                return new(response);
            }
            catch
            {
                return new(new(HttpStatusCode.NotFound));
            }
        }

        private static HttpRequestMessage CreateMessage(string url, HttpMethod method, HttpContent? content = null, Dictionary<string, string>? headers = null)
        {
            HttpRequestMessage message = new()
            {
                RequestUri = new(url),
                Method = method,
                Content = content
            };

            foreach (var it in headers ?? [])
            {
                message.Headers.Add(it.Key, it.Value);
            }

            return message;
        }

        public Http()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            clientHandler = new()
            {
                Proxy = null,
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = cookieContainer = new(),
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            httpClient = new(clientHandler, true);
        }

        public void Cookie(string domain, string value)
        {
            value
                .Replace(" ", "")
                .Split(';')
                .Select(x => x.Split('='))
                .Where(x => x.Length == 2)
                .ToList()
                .ForEach(x => cookieContainer.Add(new Cookie(x[0], x[1]) { Domain = domain }));
        }

        public void Proxy(string? address = null)
        {
            if (address == null)
            {
                clientHandler.Proxy = null;
            }
            else
            {
                clientHandler.Proxy = new WebProxy() { Address = new Uri(address), BypassProxyOnLocal = false };
            }
        }

        public void Header(string key, string value)
        {
            httpClient.DefaultRequestHeaders.Add(key, value);
        }

        public Task<HttpResponse> Get(string url, Dictionary<string, string>? headers = null)
        {
            return RequestAsync(CreateMessage(url, HttpMethod.Get, headers: headers));
        }

        public Task<HttpResponse> Head(string url, Dictionary<string, string>? headers = null)
        {
            return RequestAsync(CreateMessage(url, HttpMethod.Head, headers: headers));
        }

        public Task<HttpResponse> Post<T>(string url, T json, Dictionary<string, string>? headers = null)
        {
            return RequestAsync(CreateMessage(url, HttpMethod.Post, JsonContent.Create(json), headers: headers));
        }

        public Task<HttpResponse> Post(string url, Dictionary<string, string> form, Dictionary<string, string>? headers = null)
        {
            return RequestAsync(CreateMessage(url, HttpMethod.Post, new FormUrlEncodedContent(form), headers: headers));
        }

        public void Dispose() => httpClient.Dispose();
    }
}
