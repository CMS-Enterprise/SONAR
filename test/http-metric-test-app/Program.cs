namespace http_metric_test_app;

internal static class Program {
  public static async Task<Int32> Main(String[] args) {

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.WebHost.UseUrls("http://0.0.0.0:8082");
    var app = builder.Build();

    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();

    return 0;
  }


}

