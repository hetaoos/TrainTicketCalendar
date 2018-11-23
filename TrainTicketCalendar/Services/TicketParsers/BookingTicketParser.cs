using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services.TicketParsers
{
    public class BookingTicketParser : AbstractTicketParser
    {
        /// <summary>
        /// 名称
        /// </summary>
        public override string name { get; } = "Booking1";

        protected static Regex regexTicket = new Regex(@"([\t\d\.]{2,}|[;\d\.]{2,}|[;\t]+)(?<v>[^&;].+次列车[^\r\n]+)", RegexOptions.Compiled);
        protected static string dateFormat = "yyyy年MM月dd日HH:mm开";
        protected static Regex dateRegex = new Regex(@"(?<y>[\d]{4})年(?<m>[\d]{1,2})月(?<d>[\d]{1,2})日", RegexOptions.Compiled);

        private static Dictionary<string, TicketStateEnums> types = new Dictionary<string, TicketStateEnums>()
        {
            ["所购车票信息"] = TicketStateEnums.booking,
            ["改签后的车票信息"] = TicketStateEnums.change,
            ["所退车票信息"] = TicketStateEnums.refund
        };

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public override List<Ticket> Parse(string html)
        {
            html = html.Replace("<wbr>", "", true, CultureInfo.CurrentCulture);
            var no = GetTicketNo(html);
            if (string.IsNullOrWhiteSpace(no))
                return null;

            //—
            //1.Null，2018年05月18日18:20开，南宁东-桂林，D8280次列车,07车03A号，二等座，票价108.0元。
            //1.Null，2018年10月19日11:24开，南宁东-玉林，D8385次列车,02车12A号，二等座，票价67.0元。
            //  Null，2018年10月14日18:42开，玉林-南宁东，D8390次列车,02车17F号，二等座，票价67.0元，退票费13.5元，应退票款53.5元。
            var type = TicketStateEnums.booking;
            foreach (var v in types)
            {
                if (html.IndexOf(v.Key) > 0)
                {
                    type = v.Value;
                    break;
                }
            }

            var tickets = new List<Ticket>();
            foreach (Match m in regexTicket.Matches(html))
            {
                var arr = m.Groups["v"].Value.Split("，, ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length < 5)
                    continue;
                var p = arr[2].Split('-', '—');
                var ticket = new Ticket()
                {
                    no = no,
                    state = type,
                    name = arr[0],
                    from = new TicketStation()
                    {
                        name = p[0],
                        time = GetDateTime(html, arr[1].Trim())
                    },
                    to = new TicketStation()
                    {
                        name = p[1]
                    },
                    code = arr[3].Remove(arr[3].Length - 3),
                    seat = arr[4]
                };
                ticket.to.time = ticket.from.time.AddHours(1);
                tickets.Add(ticket);
            }
            return tickets;
            //改签后的车票信息如下
            //所购车票信息如下
            //所退车票信息如下
        }

        public DateTime GetDateTime(string html, string s)
        {
            if (s.IndexOf('年') == -1)
            {
                var m = dateRegex.Match(html);
                if (m.Success)
                    s = $"{m.Groups["y"]}年{s}";
            }
            if (s.EndsWith("开") == false)
                s += "开";
            if (DateTime.TryParseExact(s.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt;
            return new DateTime(2000, 1, 1);
        }
    }
}