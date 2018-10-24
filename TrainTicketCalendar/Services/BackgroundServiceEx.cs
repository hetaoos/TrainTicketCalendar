using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// 后台任务
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Hosting.IHostedService" />
    /// <seealso cref="System.IDisposable" />
    public abstract class BackgroundServiceEx : IHostedService, IDisposable
    {
        /// <summary>
        /// 后台任务
        /// </summary>
        protected Task _executingTask;

        /// <summary>
        /// 日志记录
        /// </summary>
        protected ILogger log;

        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        /// <summary>
        /// 失败后重启
        /// </summary>
        public bool RestartWhenFaulted { get; set; }

        /// <summary>
        /// 延时重启
        /// </summary>
        public TimeSpan RestartDelay { get; set; }

        /// <summary>
        /// 重启次数
        /// </summary>
        public int RestartTimes { get; protected set; }

        /// <summary>
        /// 最后启动时间
        /// </summary>
        public DateTime? Started { get; protected set; }

        /// <summary>
        /// 最后异常时间
        /// </summary>
        public DateTime? Faulted { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceEx"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        public BackgroundServiceEx(ILogger log = null)
        {
            this.log = log;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceEx" /> class.
        /// </summary>
        /// <param name="restartDelaySeconds">重启延时</param>
        /// <param name="log">The log.</param>
        public BackgroundServiceEx(double restartDelaySeconds, ILogger log = null)
        {
            RestartWhenFaulted = restartDelaySeconds > 0;
            RestartDelay = TimeSpan.FromSeconds(restartDelaySeconds);
            this.log = log;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceEx" /> class.
        /// </summary>
        /// <param name="restartDelay">The restart delay.</param>
        /// <param name="log">The log.</param>
        public BackgroundServiceEx(TimeSpan restartDelay, ILogger log = null)
        {
            RestartWhenFaulted = restartDelay.Ticks > 0;
            RestartDelay = restartDelay;
            this.log = log;
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            Started = DateTime.Now;
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            if (_executingTask.IsCompleted)
            {
                OnFinish(_executingTask);
                return _executingTask;
            }
            _executingTask.ContinueWith(OnFinish);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask != null)
            {
                try
                {
                    _stoppingCts.Cancel();
                }
                finally
                {
                    await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));
                }
            }
        }

        /// <summary>
        /// 任务完成后调用，包括各种结束状态
        /// </summary>
        /// <param name="task">前置任务</param>
        protected virtual void OnFinish(Task task)
        {
            if (task?.IsFaulted == true)
                Faulted = DateTime.Now;

            if (task?.IsFaulted == true && RestartWhenFaulted)
            {
                log?.LogError(task.Exception?.ToString() ?? "任务执行出错。");
                if (RestartDelay.Ticks > 0)
                {
                    var tt = Task.Delay(RestartDelay, _stoppingCts.Token);
                    try
                    {
                        tt.Wait();
                    }
                    catch
                    {
                    }
                }
                if (_stoppingCts.IsCancellationRequested == false)
                {
                    RestartTimes++;
                    log?.LogInformation("正在重启任务。");
                    StartAsync(default);
                }
            }
        }

        /// <summary>
        /// 执行与释放或重置非托管资源关联的应用程序定义的任务。
        /// </summary>
        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
        }

        /// <summary>
        /// 任务状态
        /// </summary>
        public virtual Task ExecutingTask => _executingTask;
    }
}