using Microsoft.OpenApi.Models;
using MoneyOutService.Inerfaces;
using MoneyOutService.Repositories;
using MoneyOutService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
{
    builder.Services.AddHttpClient<IClient, Client>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(30);
    }).SetHandlerLifetime(TimeSpan.FromMinutes(5));

    builder.Services.AddSingleton<IBatchService, BatchService>();    
    builder.Services.AddSingleton<IBatchRepository, BatchRepository>();
    builder.Services.AddSingleton<IBonusRepository, BonusRepository>();
    
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