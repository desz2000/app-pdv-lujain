using Microsoft.EntityFrameworkCore;
using RestaurantePDV.API.Auth;
using RestaurantePDV.API.Services;
using RestaurantePDV.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=pdv-lujain.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IExcelRelatorioService, ExcelRelatorioService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    // Migracao leve pra DB existente que ainda tem o indice unico global em Numero.
    // Substitui pelo indice unico filtrado (status=0 = Aberta), pra permitir reuso de numero.
    db.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS \"IX_Comandas_Numero\";");
    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Comandas_Numero_Aberta\" ON \"Comandas\" (\"Numero\") WHERE \"Status\" = 0;");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<PinAuthMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
