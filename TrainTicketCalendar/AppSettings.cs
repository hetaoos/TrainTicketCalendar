using System;

namespace TrainTicketCalendar
{
    public class AppSettings
    {
        /// <summary>
        /// 乘客姓名
        /// </summary>
        public string[] passengers { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? start_date { get; set; }

        /// <summary>
        /// 日历
        /// </summary>
        public CalendarSettings calendar { get; set; } = new CalendarSettings();

        /// <summary>
        /// 代理设置
        /// </summary>
        public ProxySettings proxy { get; set; } = new ProxySettings();
    }

    public class CalendarSettings
    {
        /// <summary>
        /// 日历名称
        /// </summary>
        public string name { get; set; } = "铁路行程";

        /// <summary>
        /// 默认提醒信息
        /// </summary>
        public CalendarReminder[] reminders { get; set; }
    }

    public class CalendarReminder
    {
        /// <summary>The method used by this reminder. Possible values are: - "email" - Reminders are sent via email. -
        /// "sms" - Reminders are sent via SMS. These are only available for G Suite customers. Requests to set SMS
        /// reminders for other account types are ignored. - "popup" - Reminders are sent via a UI popup. Required when
        /// adding a reminder.</summary>
        public string method { get; set; }

        /// <summary>Number of minutes before the start of the event when the reminder should trigger. Valid values are
        /// between 0 and 40320 (4 weeks in minutes). Required when adding a reminder.</summary>
        public int? minutes { get; set; }
    }

    /// <summary>
    /// 代理设置
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// 空，0，1 表示仅谷歌服务使用代理
        /// 2，表示仅12306使用代理
        /// 其他，表示全部使用代理
        /// </summary>
        public int? mode { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string password { get; set; }
    }
}