using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services
{
    public class GoogleCalendarService
    {
        private readonly ApplicationDbContext db;
        private readonly GoogleUserCredentialService googleUserCredentialService;
        private readonly LiteDB.LiteCollection<Mail> col;
        private CalendarService calendarService;
        private CalendarListEntry calendar;
        private ILogger log;

        private IOptions<AppSettings> options;

        public GoogleCalendarService(ApplicationDbContext db,
            GoogleUserCredentialService googleUserCredentialService,
            IOptions<AppSettings> options,
            ILogger<GoogleCalendarService> log)
        {
            this.db = db;
            this.options = options;
            this.log = log;

            this.googleUserCredentialService = googleUserCredentialService;
            col = db.GetCollection<Mail>();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private CalendarService InitializationCalendarService(CancellationToken stoppingToken)
        {
            if (calendarService != null)
                return calendarService;
            var initializer = googleUserCredentialService.GetInitializer(stoppingToken);
            if (initializer == null)
                return null;

            calendarService = new CalendarService(initializer);

            var name = options.Value?.calendar.name;
            if (string.IsNullOrWhiteSpace(name))
                name = "铁路行程";
            var calendarList = calendarService.CalendarList.List().Execute().Items;
            calendar = calendarService.CalendarList.List().Execute().Items.FirstOrDefault(v => v.Summary == name);
            if (calendar == null)
            {
                var c = calendarService.Calendars.Insert(new Calendar()
                {
                    Description = "铁路行程信息",
                    Summary = name,
                    TimeZone = "Asia/Shanghai",
                }).Execute();
                calendar = calendarService.CalendarList.List().Execute().Items.FirstOrDefault(v => v.Summary == name);
                if (options.Value?.calendar?.reminders?.Where(o => string.IsNullOrWhiteSpace(o?.method) == false).Any() == true) //包含提醒信息
                {
                    if (calendar.DefaultReminders == null)
                        calendar.DefaultReminders = new List<EventReminder>();
                    foreach (var v in options.Value.calendar.reminders.Where(o => string.IsNullOrWhiteSpace(o?.method) == false))
                    {
                        calendar.DefaultReminders.Add(new EventReminder() { Method = v.method, Minutes = v.minutes });
                    }
                    calendarService.CalendarList.Update(calendar, calendar.Id).Execute();
                }
            }
            return calendarService;
        }

        /// <summary>
        /// 处理邮件
        /// </summary>
        /// <returns></returns>
        public async Task ProcessMailAsync(CancellationToken stoppingToken)
        {
            var calendarService = InitializationCalendarService(stoppingToken);
            if (calendarService == null)
                return;
            //var ls = col.Find(o => o.processed != null).ToList();
            //ls.ForEach(o => { o.processed = null; o.tickets.ForEach(v => v.eventid = null); });
            //col.Update(ls);
            do
            {
                var mails = col.Find(o => o.processed == null, limit: 100).OrderBy(o => o.received).ToList();
                if (mails?.Any() != true)
                    break;

                foreach (var mail in mails)
                {
                    //退票/改签，需要找到之前的记录进行删除
                    if (mail.state == TicketStateEnums.refund || mail.state == TicketStateEnums.change)
                    {
                        var old_tockets = col.Find(o => o.no == mail.no && o.received < mail.received && o.state != TicketStateEnums.refund).SelectMany(o => o.tickets)
                            .Where(o => string.IsNullOrWhiteSpace(o.eventid) == false).ToList();
                        if (old_tockets.Any())
                        {
                            foreach (var ticket in mail.tickets)
                            {
                                var old_tocket = old_tockets.FirstOrDefault(o => o.name == ticket.name && (StationNameComparator(o.from.name, ticket.from.name) || StationNameComparator(o.to.name, ticket.to.name)));
                                if (old_tocket != null)
                                {
                                    await calendarService.Events.Delete(calendar.Id, old_tocket.eventid).ExecuteAsync();
                                    log.LogInformation($"deleted: {old_tocket}");
                                }
                            }
                        }
                    }
                    //非退票，则添加新的记录
                    if (mail.state != TicketStateEnums.refund)
                    {
                        foreach (var ticket in mail.tickets)
                        {
                            //过滤乘客
                            if (options.Value.passengers?.Any() == true && options.Value.passengers.Contains(ticket.name) == false)
                                continue;
                            var e = CreateEvent(ticket, mail);

                            e = await calendarService.Events.Insert(e, calendar.Id).ExecuteAsync();
                            ticket.eventid = e.Id;
                            log.LogInformation($"added: {ticket}");
                        }
                    }
                    mail.processed = DateTime.Now;
                    col.Update(mail);
                }
            } while (true);
        }

        protected char[] trim_chars = "站东南西北".ToArray();

        /// <summary>
        /// 判断是不是同一个城市的车站
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected bool StationNameComparator(string a, string b)
        {
            if (a == b)
                return true;
            a = a.Trim(trim_chars);
            b = b.Trim(trim_chars);
            if (a.StartsWith(b) || b.StartsWith(a))
                return true;
            return false;
        }

        public Event CreateEvent(Ticket ticket, Mail mail)
        {
            var state = ticket.state == TicketStateEnums.change ? " 改签" : "";
            //同行乘客
            string peers = null;
            if (mail.tickets.Count > 1)
            {
                var names = mail.tickets.Where(o => o.name != ticket.name && o.code == ticket.code).Select(o => o.name).ToArray();
                if (names.Any())
                    peers = $"、{ string.Join("、", names)}";
            }
            Event newEvent = new Event()
            {
                Summary = $"{ticket.code} {ticket.from.name}-{ticket.to.name} {ticket.seat}",
                Location = ticket.from.name,
                Description = $" {ticket.no} {ticket.name}{peers} {ticket.from?.time:yyyy-MM-dd HH:mm} {ticket.from?.name}-{ticket.to?.name} {ticket.code} {ticket.seat} {ticket.from.no}站台{state}",
                Start = new EventDateTime()
                {
                    DateTime = ticket.from.time,
                    TimeZone = "Asia/Shanghai",
                },
                End = new EventDateTime()
                {
                    DateTime = ticket.to.time,
                    TimeZone = "Asia/Shanghai",
                },
                //Reminders = new Event.RemindersData()
                //{
                //    UseDefault = false,
                //    Overrides = new EventReminder[] {
                //                    new EventReminder() { Method = "popup", Minutes = 70 },
                //                    new EventReminder() { Method = "popup", Minutes = 30 },
                //                }
                //},
                ExtendedProperties = new Event.ExtendedPropertiesData()
                {
                    Private__ = new Dictionary<string, string>()
                    {
                        ["no"] = ticket.no,
                        ["name"] = ticket.name,
                        ["from"] = ticket.from.name,
                        ["to"] = ticket.to.name,
                    }
                }
            };
            newEvent.Created = mail.received;

            return newEvent;
        }
    }
}