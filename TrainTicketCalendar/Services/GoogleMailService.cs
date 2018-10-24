using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;
using TrainTicketCalendar.Services.TicketParsers;

namespace TrainTicketCalendar.Services
{
    public class GoogleMailService : BackgroundServiceEx
    {
        private readonly ApplicationDbContext db;
        private readonly GoogleUserCredentialService googleUserCredentialService;
        private readonly GoogleCalendarService googleCalendarService;
        private readonly LiteDB.LiteCollection<Mail> col;
        private readonly TrainService trainService;
        private readonly ITicketParser ticketParser;
        private GmailService gmailService;
        private IOptions<AppSettings> options;

        /// <summary>
        /// 最后邮件id
        /// </summary>
        private string last_mail_id;

        public GoogleMailService(ApplicationDbContext db,
            GoogleUserCredentialService googleUserCredentialService,
            GoogleCalendarService googleCalendarService,
            TrainService trainService,
            ITicketParser ticketParser,
            IOptions<AppSettings> options,
            ILogger<GoogleMailService> log)
            : base(log)
        {
            this.db = db;
            this.googleUserCredentialService = googleUserCredentialService;
            this.googleCalendarService = googleCalendarService;
            this.trainService = trainService;
            this.ticketParser = ticketParser;
            this.options = options;

            col = db.GetCollection<Mail>();
            col.EnsureIndex(o => o.id);
            col.EnsureIndex(o => o.no);
            col.EnsureIndex(o => o.received);
            col.EnsureIndex(o => o.processed);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var initializer = googleUserCredentialService.GetInitializer(stoppingToken);
            if (initializer == null)
                return;

            while (db.GetCollection<Station>().Count() == 0 || db.GetCollection<Train>().Count() == 0)
            {
                log.LogInformation("waiting for basic data updates.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            await googleCalendarService.ProcessMailAsync(stoppingToken);
            log.LogInformation("fetching new mail...");
            gmailService = new GmailService(initializer);

            while (stoppingToken.IsCancellationRequested == false)
            {
                var last_mail = col.FindAll().OrderByDescending(o => o.received).Select(o => new { o.id, o.received }).FirstOrDefault();
                var dt = last_mail?.received ?? new DateTime(2000, 1, 1);
                if (string.IsNullOrWhiteSpace(last_mail_id) && string.IsNullOrWhiteSpace(last_mail?.id) == false)
                    last_mail_id = last_mail.id;
                if (dt.Year <= 2000)
                    dt = options.Value.start_date ?? new DateTime(DateTime.Now.Year - 1, 1, 1);
                var request = gmailService.Users.Messages.List("me");
                //request.MaxResults = 100;
                var ts = (long)(dt.ToUniversalTime() - DateTimeOffset.UnixEpoch).TotalSeconds;
                request.Q = $"from:12306@rails.com.cn after:{ts}";
                var resp = await request.ExecuteAsync();

                if (resp.Messages?.Any() != true)
                    goto sleep;

                if (resp.Messages.Count == 1 && resp.Messages[0].Id == last_mail_id)
                    goto sleep;

                var messages = new List<Message>();

                messages.AddRange(resp.Messages);
                while (string.IsNullOrWhiteSpace(resp.NextPageToken) == false)
                {
                    request.PageToken = resp.NextPageToken;
                    resp = await request.ExecuteAsync();
                    if (resp.Messages?.Any() != true)
                        break;
                    messages.AddRange(resp.Messages);
                }
                messages.Reverse();
                var mails = new List<Mail>();
                foreach (var msg in messages)
                {
                    var mail = await ParseMessageAsync(msg, stoppingToken);
                    if (mail != null)
                    {
                        mails.Add(mail);
                    }
                }

                if (mails.Any() != true)
                    goto sleep;
                last_mail_id = mails.Last().id;
                var ids = mails.Select(o => o.id).ToList();
                ids = col.Find(o => ids.Contains(o.id)).Select(o => o.id).ToList();
                if (ids.Any())
                    mails.RemoveAll(o => ids.Contains(o.id));

                if (mails.Any())
                    col.InsertBulk(mails);
                await googleCalendarService.ProcessMailAsync(stoppingToken);
                sleep:
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        /// <summary>
        /// 转换邮件
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task<Mail> ParseMessageAsync(Message msg, CancellationToken stoppingToken)
        {
            Console.WriteLine("{0}", msg.Id);
            var mail = gmailService.Users.Messages.Get("me", msg.Id).Execute();
            var body = mail.Payload.Parts[0].Body.Data;
            String codedBody = body.Replace("-", "+");
            codedBody = codedBody.Replace("_", "/");
            var data = Convert.FromBase64String(codedBody);
            var str = Encoding.UTF8.GetString(data);
            var ls = ticketParser.Parse(str);
            if (ls?.Any() == true)
            {
                foreach (var t in ls)
                {
                    var trainSchedule = await trainService.GetTrainScheduleAsync(t.code, t.from.time, stoppingToken);
                    if (trainSchedule != null)
                    {
                        var start = trainSchedule.stations.FirstOrDefault(o => o.station_name == t.from.name);
                        var end = trainSchedule.stations.FirstOrDefault(o => o.station_name == t.to.name);
                        t.from.no = start?.station_no;
                        t.to.no = end?.station_no;
                        t.to.time = (end?.arrive_time ?? end?.start_time) ?? t.from.time.AddHours(1);
                    }
                    log.LogInformation($"id={msg.Id} {t.ToString()}");
                }

                var first = ls[0];
                return new Mail()
                {
                    received = DateTimeOffset.FromUnixTimeMilliseconds(mail.InternalDate.Value).DateTime.ToLocalTime(),
                    created = DateTime.Now,
                    id = msg.Id,
                    no = first.no,
                    state = first.state,
                    tickets = ls
                };
            }
            else
            {
                log.LogError($"mail parse error: {msg.Id} {DateTimeOffset.FromUnixTimeMilliseconds(mail.InternalDate.Value).DateTime.ToLocalTime()}");
            }
            return null;
        }
    }
}