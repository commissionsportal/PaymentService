using Microsoft.OpenApi.Models;
using PaymentService;
using PaymentService.Interfaces;
using PaymentService.Options;
using PaymentService.Repositories;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.Configure<PaymentureMoneyOutServiceOptions>(builder.Configuration.GetSection("PaymentureMoneyOutService"));

    builder.Services.AddHttpClient<Client>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddHttpMessageHandler<ClientMessageHandler>();

    builder.Services.AddSingleton<IBatchService, BatchService>();    
    builder.Services.AddSingleton<IBonusRepository, BonusRepository>();
    builder.Services.AddSingleton<IPaymentureWalletService, PaymentureWalletService>();
    
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Money Out Service", Version = "v1" });
    });
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Money Out Service v1"));

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}

app.Run();