using Google.Apis.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;

namespace TrainTicketCalendar.Services
{
    public class RailsApiService
    {
        private readonly HttpClient client;
        private IOptions<AppSettings> options;
        private readonly ILogger log;

        public RailsApiService(IOptions<AppSettings> options, ILogger<RailsApiService> log)
        {
            this.options = options;
            this.log = log;
            var httpClientHandler = ProxySupportedHttpClientFactory.GetHttpClientHandler(options.Value?.proxy, false);
            client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Referrer = new Uri("https://kyfw.12306.cn/otn/leftTicket/init");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.67 Safari/537.36");
        }

        /// <summary>
        /// 获取站点信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<Station>> GetStationsAsync(CancellationToken cancellationToken)
        {
            var url = "https://kyfw.12306.cn/otn/resources/js/framework/station_name.js";
            log.LogInformation($"starting download station info.");
            var resp = await client.GetAsync(url, cancellationToken);
            if (resp.IsSuccessStatusCode == false)
            {
                log.LogError($"downlaod station error: {resp.ReasonPhrase}");
                return null;
            }
            string js = await resp.Content.ReadAsStringAsync();
            var start = js.IndexOf('\'');
            var end = js.LastIndexOf('\'');
            var d = js.Substring(start + 1, end - start - 1);
            var items = d.Split('@', StringSplitOptions.RemoveEmptyEntries);

            var stations = new ConcurrentBag<Station>();
            items.AsParallel()
                .ForAll(item =>
                {
                    var values = item.Split('|');
                    if (values.Length != 6)
                        return;
                    int i = 0;
                    var station = new Station()
                    {
                        first_letter = values[i++],
                        name = values[i++],
                        code = values[i++],
                        pinyin = values[i++],
                        shorthand = values[i++],
                        order = int.Parse(values[i++]),
                    };
                    stations.Add(station);
                });

            log.LogInformation($"station downloaded: {stations.Count}");
            return stations.OrderBy(o => o.order).ToList();
        }

        /// <summary>
        /// 获取车次信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<Train>> GetTrainAsync(CancellationToken cancellationToken)
        {
            var url = "https://kyfw.12306.cn/otn/resources/js/query/train_list.js";
            log.LogInformation($"starting download train info.");
            var resp = await client.GetAsync(url, cancellationToken);
            if (resp.IsSuccessStatusCode == false)
            {
                log.LogError($"downlaod train error: {resp.ReasonPhrase}");
                return null;
            }
            string js = await resp.Content.ReadAsStringAsync();
            //File.WriteAllText("train_list.js", js);
            //var js = File.ReadAllText("train_list.js");
            var start = js.IndexOf('=');
            js = js.Substring(start + 1);

            var jObj = JObject.Parse(js);
            var dates = jObj.Properties().Select(p => p.Name).ToList();
            var separator = "()-".ToArray();
            var trains = new ConcurrentDictionary<string, Train>();

            dates.AsParallel().ForAll(date =>
            {
                var dt = DateTime.Parse(date);

                JObject dateObj = (JObject)jObj[date];
                List<string> types = dateObj.Properties().Select(p => p.Name).ToList();
                foreach (string type in types)
                {
                    JArray dataArray = (JArray)dateObj[type];
                    foreach (JObject data in dataArray)
                    {
                        string train_no = data["train_no"].ToString(); //24000000D10Y
                        string station_train_code = data["station_train_code"].ToString(); //D1(北京-沈阳)
                        var values = station_train_code.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
                        var key = $"{train_no}{values[0]}";
                        trains.AddOrUpdate(key, (_key) => new Train()
                        {
                            no = train_no,
                            type = type,
                            code = values[0],
                            from = values[1],
                            to = values[2],
                            dates = new List<DateTime>() { dt }
                        }, (_key, old) => old.AddDate(dt));
                    }
                }
            });
            log.LogInformation($"train downloaded: {trains.Count}");
            return trains.Values.OrderBy(o => o.no).ThenBy(o => o.code).ToList();
        }

        private static readonly Regex regexStopoverTime = new Regex(@"[\d]+");

        /// <summary>
        /// 获取车次经停信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TrainSchedule> GetTrainScheduleAsync(string train_no, string from_station_telecode, string to_station_telecode, DateTime depart_date, CancellationToken cancellationToken)
        {
            depart_date = depart_date.Date;
            var url = $"https://kyfw.12306.cn/otn/czxx/queryByTrainNo?train_no={train_no}&from_station_telecode={from_station_telecode}&to_station_telecode={to_station_telecode}&depart_date={depart_date:yyyy-MM-dd}";
            log.LogInformation($"starting download train schedule info: {train_no} {depart_date:yyyy-MM-dd}");
            var resp = await client.GetAsync(url, cancellationToken);
            if (resp.IsSuccessStatusCode == false)
            {
                log.LogError($"downlaod train schedule error: {resp.ReasonPhrase}");
                return null;
            }
            string js = await resp.Content.ReadAsStringAsync();

            var jObj = JObject.Parse(js);
            if (jObj["httpstatus"].ToString() != "200")
                return null;
            var dataArray = (JArray)jObj["data"]["data"];
            if (dataArray?.Any() != true)
                return null;
            var first = (JObject)dataArray[0];
            var train = new TrainSchedule()
            {
                no = train_no,
                code = first["station_train_code"].ToString(),
                day = depart_date.Date,
                from = first["start_station_name"].ToString(),
                to = first["end_station_name"].ToString(),
                start_time = depart_date.Add(TimeSpan.Parse(first["start_time"].ToString())),
                stations = new List<TrainStation>()
            };
            TimeSpan last_time = TimeSpan.Parse(first["start_time"].ToString());
            TimeSpan time;
            foreach (var data in dataArray)
            {
                var station = new TrainStation()
                {
                    station_name = data["station_name"].ToString(), //永州
                    station_no = data["station_no"].ToString(),//06
                };
                string str_stopover_time = data["stopover_time"].ToString();
                var m = regexStopoverTime.Match(str_stopover_time);
                if (m.Success)
                    station.stopover_time = int.Parse(m.Value);
                string str_arrive_time = data["arrive_time"].ToString(); //12:05 // "----"

                if (TimeSpan.TryParse(str_arrive_time, out time))
                {
                    if (time < last_time) //跨天
                        depart_date = depart_date.AddDays(1);
                    station.arrive_time = depart_date.Add(time);
                    last_time = time;
                }
                string str_start_time = data["start_time"].ToString(); // 12:09
                if (TimeSpan.TryParse(str_start_time, out time))
                {
                    station.start_time = depart_date.Add(time);
                }
                train.stations.Add(station);
            }
            train.end_time = train.stations.Last().arrive_time ?? train.stations.Last().start_time;
            //log.LogInformation($"train downloaded: {trains.Count}");
            return train;
        }
    }
}