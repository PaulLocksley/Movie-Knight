
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Movie_Knight.Models;
using Prometheus;
using SlackLogger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("RequestClient", x =>
{
    x.BaseAddress = new Uri("https://letterboxd.com/");
    x.Timeout = TimeSpan.FromSeconds(90);
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRazorPages();
builder.Services.AddMetricServer(options =>
{
    options.Port = 4010;
});

var loggingUrl = Environment.GetEnvironmentVariable("LOGGING_WEBHOOK_URL");
if (loggingUrl is not null)
{
    builder.Logging.AddSlack(options =>
    {
        options.WebhookUrl = $"{loggingUrl}/slack";
        options.NotificationLevel = LogLevel.Error;
    });

}
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Logger.LogWarning("Movie-Knight has started");
app.UseExceptionHandler(errorApp =>
   {
       errorApp.Run(async context =>
       {
           var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
           var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
           if (exception is not null)
           {
               logger.LogError($"{exception.Message} - {exception.StackTrace?.Split("\n")[0]}");
           }
           context.Response.StatusCode = 500;
           context.Response.ContentType = "text/plain";
           await context.Response.WriteAsync("Sorry about that, an unexpected error occurred. Please try again later.");
       });
   });

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpMetrics();
app.MapRazorPages();
app.UseAuthorization();
app.MapControllers();

app.Run();