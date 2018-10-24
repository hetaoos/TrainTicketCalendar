using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services.TicketParsers
{
    /// <summary>
    ///
    /// </summary>
    public abstract class AbstractTicketParser : ITicketParser
    {
        public abstract string name { get; }

        protected static Regex regexTicketNo = new Regex(@"[> 码](?<no>[A-Z]{1,4}[\d]{8,10})[< 。]", RegexOptions.Compiled);

        public abstract List<Ticket> Parse(string html);

        /// <summary>
        /// 获取票号
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected string GetTicketNo(string html)
        {
            //订单号码 EC72959272
            var m = regexTicketNo.Match(html);
            if (m.Success)
                return m.Groups["no"].Value.Trim();
            return null;
        }
    }
}