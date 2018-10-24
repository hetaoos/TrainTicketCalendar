using System.Collections.Generic;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services.TicketParsers
{
    /// <summary>
    /// 车票解析器
    /// </summary>
    public interface ITicketParser
    {
        /// <summary>
        /// 名称
        /// </summary>
        string name { get; }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        List<Ticket> Parse(string html);
    }
}