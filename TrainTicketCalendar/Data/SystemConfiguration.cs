using LiteDB;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 配置项
    /// </summary>
    public class SystemConfiguration
    {
        /// <summary>
        /// 唯一id
        /// </summary>
        [BsonId(false)]
        public string id { get; set; }

        /// <summary>
        /// json 值
        /// </summary>
        public string json { get; set; }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        public SystemConfiguration SetValue<TValue>(TValue value)
        {
            json = value == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(value);
            return this;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public TValue GetValue<TValue>() => json == null ? default(TValue) : Newtonsoft.Json.JsonConvert.DeserializeObject<TValue>(json);
    }
}