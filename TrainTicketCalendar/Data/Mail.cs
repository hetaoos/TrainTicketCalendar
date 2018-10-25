using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 邮件
    /// </summary>
    public class Mail
    {
        /// <summary>
        /// 邮件id
        /// </summary>
        [BsonId(false)]
        public string id { get; set; }

        /// <summary>
        /// 邮件接收时间
        /// </summary>
        public DateTime received { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string no { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public TicketStateEnums state { get; set; }

        /// <summary>
        /// 票信息
        /// </summary>
        public List<Ticket> tickets { get; set; }

        /// <summary>
        /// 邮件处理日期
        /// </summary>
        public DateTime? processed { get; set; }

        /// <summary>
        /// 解析时间
        /// </summary>
        public DateTime created { get; set; }

        public override string ToString() => $"{no} {received:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// 车票
    /// </summary>
    public class Ticket
    {
        /// <summary>
        /// 状态
        /// </summary>
        public TicketStateEnums state { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string no { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 车次
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 出发站
        /// </summary>
        public TicketStation from { get; set; }

        /// <summary>
        /// 到达站
        /// </summary>
        public TicketStation to { get; set; }

        /// <summary>
        /// 日历id，用于退票和改签时候删除日历
        /// </summary>
        public string eventid { get; set; }

        /// <summary>
        /// 座位
        /// </summary>
        public string seat { get; set; }

        public override string ToString()
        {
            return $"{name} {from?.time:yyyy-MM-dd HH:mm} {from?.name}-{to?.name} {code} {seat} {no} {state}";
        }
    }

    /// <summary>
    /// 车票的车站信息
    /// </summary>
    public class TicketStation
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 出发或者到达时间
        /// </summary>
        public DateTime time { get; set; }

        public override string ToString() => $"{name} {time:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// 票状态类型
    /// </summary>
    public enum TicketStateEnums
    {
        /// <summary>
        /// 预定
        /// </summary>
        [Description("预定")]
        booking,

        /// <summary>
        /// 改签
        /// </summary>
        [Description("改签")]
        change,

        /// <summary>
        /// 退票
        /// </summary>
        [Description("退票")]
        refund,
    }
}