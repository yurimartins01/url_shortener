using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Endpoints;
using UrlShortener.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();

builder.Services.AddScoped<ShortLinkService>();

var app = builder.Build();

{ 

    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();

ShortLinkEndpoints.Map(app);

app.Run();