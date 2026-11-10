using MarksApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
     .UseSnakeCaseNamingConvention());

var app = builder.Build();
app.MapGet("/", () => "MarksApi running");
app.Run();