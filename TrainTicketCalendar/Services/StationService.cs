using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services
{
    /// <summary>
    /// 车站服务
    /// </summary>
    public class StationService : BackgroundServiceEx
    {
        private readonly ApplicationDbContext db;
        private readonly RailsApiService railsApiService;
        private readonly LiteDB.LiteCollection<Station> col;
        private Config cfg;

        public StationService(ApplicationDbContext db,
            RailsApiService railsApiService,
            ILogger<StationService> log)
            : base(log)
        {
            this.db = db;
            this.railsApiService = railsApiService;
            col = db.GetCollection<Station>();
            col.EnsureIndex(o => o.code);
            col.EnsureIndex(o => o.order);
            col.EnsureIndex(o => o.name);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cfg = db.GetConfig(() => new Config());

            while (stoppingToken.IsCancellationRequested == false)
            {
                var dt = cfg.last_updated.Add(cfg.interval);
                if (dt < DateTime.Now) //下次更新时间比现在的小
                {
                    await DownlaodStationAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
                else
                {
                    await Task.Delay(dt - DateTime.Now, stoppingToken);
                }
            }
            return;
        }

        /// <summary>
        /// 车站名转换为代码
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string NameToCode(string name) => col.Find(o => o.name == name, limit: 1).Select(o => o.code).FirstOrDefault();

        /// <summary>
        /// 下载车站信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task DownlaodStationAsync(CancellationToken cancellationToken)
        {
            var stations = await railsApiService.GetStationsAsync(cancellationToken);

            if (stations?.Any() != true)
                return;
            col.Delete(o => true);
            col.InsertBulk(stations.OrderBy(o => o.order));

            if (cfg == null)
                cfg = new Config() { last_updated = DateTime.Now };
            else
                cfg.last_updated = DateTime.Now;
            db.SetConfig(cfg);
        }

        /// <summary>
        /// 配置
        /// </summary>
        public class Config
        {
            /// <summary>
            /// 最后更新时间
            /// </summary>
            public DateTime last_updated { get; set; }

            /// <summary>
            /// 更新间隔
            /// </summary>
            public TimeSpan interval { get; set; } = TimeSpan.FromDays(1);
        }
    }
}