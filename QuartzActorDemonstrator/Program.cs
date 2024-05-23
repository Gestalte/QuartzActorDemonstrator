using Akka.Actor;
using Akka.Hosting;
using Akka.Logger.NLog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QuartzActorDemonstrator
{
    internal static class Program
    {
        internal static IOptionsMonitor<SchedulerSettings>? SchedulerSettingsMonitor;

        static void Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.Configure<SchedulerSettings>(config.GetSection("Scheduler"));

            builder.Services.AddAkka("service-janitor-actor-system", cb =>
            {
                cb
                .ConfigureLoggers(setup =>
                {
                    setup.LogLevel = Akka.Event.LogLevel.InfoLevel;
                    setup.ClearLoggers();
                    setup.AddLogger<NLogLogger>();
                    setup.DebugOptions = new DebugOptions
                    {
                        Receive = true,
                        LifeCycle = true,
                        RouterMisconfiguration = true,
                        Unhandled = true
                    };
                    setup.LogConfigOnStart = true;
                })
                .WithActors((system, registry) =>
                {
                    Props props = Props.Create(() => new TopLevelSupervisor());
                    _ = system.ActorOf(props, "top-level-supervisor");
                });
            });

            var host = builder.Build();

            SchedulerSettingsMonitor = host.Services.GetRequiredService<IOptionsMonitor<SchedulerSettings>>();

            host.Run();
        }
    }
}
