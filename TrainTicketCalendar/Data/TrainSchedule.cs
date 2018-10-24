using LiteDB;
using System;
using System.Collections.Generic;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 列车时刻表
    /// </summary>
    public class TrainSchedule
    {
        /// <summary>
        /// id
        /// </summary>
        [BsonId(false)]
        public string id { get => GetId(code, day); set { } }

        /// <summary>
        /// 车号 7100000K220R
        /// </summary>
        public string no { get; set; }

        /// <summary>
        /// 车次 D1
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime day { get; set; }

        /// <summary>
        /// 出发站 北京
        /// </summary>
        public string from { get; set; }

        /// <summary>
        /// 到达站 沈阳
        /// </summary>
        public string to { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>

        public DateTime start_time { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime end_time { get; set; }

        /// <summary>
        /// 经停站点信息
        /// </summary>
        public List<TrainStation> stations { get; set; }

        /// <summary>
        /// 生成id
        /// </summary>
        /// <param name="code"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static string GetId(string code, DateTime day) => $"{day:yyyyMMdd}{code}";

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{from}-{to} {code} {start_time:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// 列车停靠站点
    /// </summary>
    public class TrainStation
    {
        /// <summary>
        /// 到达时间
        /// </summary>
        public DateTime? arrive_time { get; set; }

        /// <summary>
        /// 站点名称
        /// </summary>
        public string station_name { get; set; }

        /// <summary>
        /// 发车时间
        /// </summary>
        public DateTime start_time { get; set; }

        /// <summary>
        /// 停靠时间,分钟
        /// </summary>
        public int? stopover_time { get; set; }

        /// <summary>
        /// 站台号
        /// </summary>
        public string station_no { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{arrive_time ?? start_time:yyyy-MM-dd HH:mm} {station_name}";
    }
}