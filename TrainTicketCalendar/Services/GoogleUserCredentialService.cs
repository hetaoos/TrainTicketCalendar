using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services
{
    public class GoogleUserCredentialService
    {
        public static readonly string[] Scopes = { GmailService.Scope.GmailReadonly, CalendarService.Scope.Calendar };
        public static readonly string ApplicationName = "12306 Ticket to Google Calendar";

        private readonly ApplicationDbContext db;
        private readonly IDataStore dataStore;
        private UserCredential credential;
        private readonly ILogger log;
        private IOptions<AppSettings> options;

        public GoogleUserCredentialService(ApplicationDbContext db,
            IDataStore dataStore,
            IOptions<AppSettings> options,
            ILogger<GoogleUserCredentialService> log)
        {
            this.db = db;
            this.dataStore = dataStore;
            this.options = options;
            this.log = log;
        }

        /// <summary>
        /// 获取令牌
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public UserCredential GetUserCredential(CancellationToken stoppingToken = default)
        {
            if (credential != null)
                return credential;
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                var initializer = new GoogleAuthorizationCodeFlow.Initializer()
                {
                    HttpClientFactory = new ProxySupportedHttpClientFactory(options.Value?.proxy),
                    ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
                };
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    initializer,
                    Scopes,
                    "user",
                    stoppingToken,
                    dataStore).Result;
            }

            return credential;
        }

        /// <summary>
        /// 获取初始化器
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public BaseClientService.Initializer GetInitializer(CancellationToken stoppingToken = default)
        {
            var credential = GetUserCredential(stoppingToken);
            if (credential == null)
            {
                log.LogCritical($"failed to get the UserCredential.");
                return null;
            }

            return new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
                HttpClientFactory = new ProxySupportedHttpClientFactory(options.Value?.proxy),
            };
        }
    }
}