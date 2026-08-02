using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();