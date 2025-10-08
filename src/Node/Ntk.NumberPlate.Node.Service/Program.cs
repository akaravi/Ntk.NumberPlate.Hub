using Ntk.NumberPlate.Node.Service;
using Ntk.NumberPlate.Node.Service.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/node-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("شروع سرویس Node...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Ntk NumberPlate Node Service";
    });

    builder.Services.AddSerilog();

    // Add Services
    builder.Services.AddSingleton<ConfigurationService>();
    builder.Services.AddSingleton<YoloDetectionService>();
    builder.Services.AddSingleton<HubCommunicationService>();
    builder.Services.AddSingleton<SpeedCalculationService>();
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "خطای حیاتی در سرویس Node");
    throw;
}
finally
{
    Log.CloseAndFlush();
}


