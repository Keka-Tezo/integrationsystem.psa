using oculusit.sync.connectwise;
using oculusit.sync.keka;
using oculusit.sync.orchestration;

namespace oculusit.sync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
           
            builder.Services.AddKekaServices(builder.Configuration);
            builder.Services.AddConnectWiseServices(builder.Configuration);
            builder.Services.AddOrchestrationServices();

            builder.Services.AddHostedService<Worker>();
            var host = builder.Build();
            host.Run();
        }
    }
}
