using LiteDB;
using System;
using System.Collections.Generic;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 列车信息
    /// </summary>
    public class Train
    {
        /// <summary>
        /// id
        /// </summary>
        [BsonId(false)]
        public string id { get => $"{no}{code}"; set { } }

        /// <summary>
        /// 车号 24000000D10R
        /// </summary>
        public string no { get; set; }

        /// <summary>
        /// 类型 C D G K O T Z
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// 车次 D1
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 出发站 北京
        /// </summary>
        public string from { get; set; }

        /// <summary>
        /// 到达站 沈阳
        /// </summary>
        public string to { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public List<DateTime> dates { get; set; }

        public Train AddDate(DateTime dt)
        {
            if (dates == null)
                dates = new List<DateTime>();

            dates.Add(dt);

            return this;
        }

        public override string ToString() => $"{code} {no} {from}-{to}";
    }
}