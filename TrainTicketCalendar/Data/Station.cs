using LiteDB;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 车站信息
    /// </summary>
    public class Station
    {
        /// <summary>
        /// id
        /// </summary>
        [BsonId(false)]
        public string code { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 首字母
        /// </summary>
        public string first_letter { get; set; }

        /// <summary>
        /// 拼音
        /// </summary>
        public string pinyin { get; set; }

        /// <summary>
        /// 缩写
        /// </summary>
        public string shorthand { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{code} {name}";
    }
}