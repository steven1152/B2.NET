using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace B2Net.Http {
	public static class HttpClientFactory {
	    private static HttpClient _client;
		public static IWebProxy Proxy { get; set; }

        public static HttpClient CreateHttpClient(int timeout) {
            if (_client == null) {
				var handler = new HttpClientHandler() { AllowAutoRedirect = true, Proxy = Proxy };

				_client = new HttpClient(handler, true);

                _client.Timeout = TimeSpan.FromSeconds(timeout);

                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            return _client;
        }
	}
}
