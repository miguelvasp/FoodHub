using System.Text.Json.Serialization;
using FoodHub.Orders.Data;
using FoodHub.Orders.Data.Repositories;
using FoodHub.Orders.Api.Middleware;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(options.DatabaseName);
});
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var httpsRedirectionEnabled = builder.Configuration.GetValue<bool?>("HttpsRedirection:Enabled") ?? true;
if (httpsRedirectionEnabled)
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
