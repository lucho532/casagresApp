using Casagres.API.Data;
using Microsoft.EntityFrameworkCore;
using Casagres.API.Services;
using Casagres.API.Services.Pronostico;
using Casagres.API.Middleware;
using Casagres.API;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        var frontendUrl =
            builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";

        policy
            .WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<RutasDatosService>();
builder.Services.AddScoped<IPronosticoCsvService, PronosticoCsvService>();
builder.Services.AddScoped<IPronosticoIntervalosCsvService, PronosticoIntervalosCsvService>();
builder.Services.AddScoped<IMetodosCsvService, MetodosCsvService>();
builder.Services.AddScoped<IDashboardPronosticoService, DashboardPronosticoService>();
builder.Services.AddScoped<IHistoricoVentasService, HistoricoVentasService>();
builder.Services.AddSingleton<IActualizacionService, ActualizacionService>();
builder.Services.AddSingleton<ActualizacionEstadoService>();
builder.Services.AddHostedService<MonitorOneDriveService>();
builder.Services.AddSingleton<IProductoService, ProductoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
//Autenticacion 
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var clave = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(clave!)
                ),

            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IMicrosoftAuthService, MicrosoftAuthService>();
builder.Services.AddSingleton<IGraphService, GraphService>();

// Conexión con PostgreSQL
builder.Services.AddDbContext<CasagresDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CadenaPostgres")
    )
);

var app = builder.Build();
app.UseAuthentication();
app.UseMiddleware<RevalidarUsuarioMiddleware>();
app.UseAuthorization();
app.UseCors("ReactPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();