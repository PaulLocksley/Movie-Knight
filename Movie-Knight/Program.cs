
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Movie_Knight.Models;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.UseAuthorization();

app.MapControllers();

app.Run();