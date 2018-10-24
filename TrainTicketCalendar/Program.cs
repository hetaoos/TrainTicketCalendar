using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TrainTicketCalendar.Data;
using TrainTicketCalendar.Google;
using TrainTicketCalendar.Services;
using TrainTicketCalendar.Services.TicketParsers;

namespace TrainTicketCalendar
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var isService = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !(Debugger.IsAttached || args?.Contains("--service") == false);
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AppSettings>(hostContext.Configuration.GetSection("settings"));
                    services.AddSingleton(provider => new ApplicationDbContext(hostContext.Configuration.GetConnectionString("DefaultConnection")));
                    services.AddSingleton<IDataStore, DbDataStore>();
                    services.AddSingleton<ITicketParser, BookingTicketParser>();
                    services.AddSingleton<RailsApiService>();
                    services.AddHostedServiceEx<StationService>();
                    services.AddHostedServiceEx<TrainService>();
                    services.AddSingleton<GoogleUserCredentialService>();
                    services.AddSingleton<GoogleCalendarService>();
                    services.AddHostedServiceEx<GoogleMailService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    if (isService == false)
                        configLogging.AddConsole();
                    configLogging.AddDebug();
                    configLogging.AddFile("Logs/{Date}.log");
                });

            if (isService)
            {
                await host.RunAsServiceAsync();
            }
            else
            {
                await host.RunConsoleAsync();
            }
        }
    }
}