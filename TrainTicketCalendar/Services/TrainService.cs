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
    public class TrainService : BackgroundServiceEx
    {
        private readonly ApplicationDbContext db;
        private readonly LiteDB.LiteCollection<Train> col;
        private readonly LiteDB.LiteCollection<TrainSchedule> colTrainSchedule;
        private readonly RailsApiService railsApiService;
        private Config cfg;

        public TrainService(ApplicationDbContext db,
            RailsApiService railsApiService,
            ILogger<TrainService> log)
            : base(log)
        {
            this.db = db;
            this.railsApiService = railsApiService;
            col = db.GetCollection<Train>();
            col.EnsureIndex(o => o.no);
            col.EnsureIndex(o => o.code);

            colTrainSchedule = db.GetCollection<TrainSchedule>();
            colTrainSchedule.EnsureIndex(o => o.code);
            colTrainSchedule.EnsureIndex(o => o.no);
            colTrainSchedule.EnsureIndex(o => o.day);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cfg = db.GetConfig(() => new Config());
            //var r = await GetTrainScheduleAsync("7100000K220R", DateTime.Now.AddDays(10), stoppingToken);
            //var r2 = await GetTrainScheduleAsync("D8280", new DateTime(2018, 10, 26), stoppingToken);
            while (stoppingToken.IsCancellationRequested == false)
            {
                var dt = cfg.last_updated.Add(cfg.interval);
                if (dt < DateTime.Now) //下次更新时间比现在的小
                {
                    await DownlaodTrainAsync(stoppingToken);
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
        /// 查询列车经停信息
        /// </summary>
        /// <param name="train_no_or_code">车号或者车次</param>
        /// <param name="day">日期</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TrainSchedule> GetTrainScheduleAsync(string train_no_or_code, DateTime day, CancellationToken cancellationToken)
        {
            train_no_or_code = train_no_or_code.Trim();
            day = day.Date;
            var trainSchedule = colTrainSchedule.FindOne(o => o.day == day && (o.no == train_no_or_code || o.code == train_no_or_code));
            if (trainSchedule != null)
                return trainSchedule;
            do
            {
                var train = col.Find(o => o.code == train_no_or_code || o.no == train_no_or_code, limit: 1).Select(o => new { o.no, o.from }).FirstOrDefault();
                if (train == null)
                    break;
                var code = db.GetCollection<Station>().Find(o => o.name == train.from).Select(o => o.code).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(code))
                    break;
                trainSchedule = await railsApiService.GetTrainScheduleAsync(train.no, code, code, day, cancellationToken);
                if (trainSchedule == null)
                    break;
                colTrainSchedule.Upsert(trainSchedule);
            } while (false);
            //从历史记录中找一条
            if (trainSchedule == null)
                trainSchedule = colTrainSchedule.FindOne(o => (o.no == train_no_or_code || o.code == train_no_or_code));
            return trainSchedule;
        }

        /// <summary>
        /// 下载车站信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task DownlaodTrainAsync(CancellationToken cancellationToken)
        {
            var trains = await railsApiService.GetTrainAsync(cancellationToken);
            if (trains?.Any() != true)
                return;
            col.Delete(o => true);
            col.InsertBulk(trains);

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