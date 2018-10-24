using System.Net;
using System.Net.Http;
using TrainTicketCalendar;

namespace Google.Apis.Http
{
    public class ProxySupportedHttpClientFactory : HttpClientFactory
    {
        private ProxySettings proxy;

        public ProxySupportedHttpClientFactory(ProxySettings proxy)
        {
            this.proxy = proxy;
        }

        protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => GetHttpClientHandler(proxy, true);

        public static HttpClientHandler GetHttpClientHandler(ProxySettings proxy, bool google)
        {
            if (string.IsNullOrWhiteSpace(proxy?.address))
                return null;

            var use = google ? proxy.mode != 2 : proxy.mode != null && proxy.mode != 0 && proxy.mode != 1;

            var httpClientHandler = new HttpClientHandler()
            {
                UseCookies = false,
            };

            if (use == false)
                return httpClientHandler;

            ICredentials credentials = null;
            if (string.IsNullOrWhiteSpace(proxy.username) == false && string.IsNullOrEmpty(proxy.password) == false)
                credentials = new NetworkCredential(proxy.username, proxy.password);
            var webProxy = new WebProxy(proxy.address, true, null, credentials);

            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = webProxy;

            return httpClientHandler;
        }
    }
}