using LiteDB;
using System;
using System.Linq;

namespace TrainTicketCalendar.Data
{
    /// <summary>
    /// 数据库访问实体
    /// </summary>
    public class ApplicationDbContext : LiteDatabase
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="connectionString"></param>
        public ApplicationDbContext(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// 获取配置项目名称
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        private string GetConfigId<TValue>()
        {
            var _type = typeof(TValue);
            if (_type.IsGenericType == false)
                return _type.FullName;

            var bType = _type.GetGenericTypeDefinition();
            return bType.FullName + ":" + string.Join("_", _type.GetGenericArguments()
                      .Select(o => o.FullName));
        }

        /// <summary>
        /// 获取配置项
        /// </summary>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="newValueFactory">获取默认值</param>
        /// <param name="desc">创建时设置描述信息</param>
        /// <returns></returns>
        public TValue GetConfig<TValue>(Func<TValue> newValueFactory = null)
        {
            var id = GetConfigId<TValue>();
            var item = GetCollection<SystemConfiguration>().FindOne(o => o.id == id);

            if (item != null)
                return item.GetValue<TValue>();

            if (newValueFactory != null)
            {
                var value = newValueFactory();
                SetConfig(value);
                return value;
            }
            else
                return default(TValue);
        }

        /// <summary>
        /// 设置配置项
        /// </summary>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="value">值</param>
        /// <param name="id">id</param>
        /// <param name="desc">描述信息</param>
        public bool SetConfig<TValue>(TValue value)
        {
            var id = GetConfigId<TValue>();
            return GetCollection<SystemConfiguration>().Upsert(id, new SystemConfiguration() { id = id }.SetValue(value));
        }
    }
}