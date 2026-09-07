using Casagres.API.Data;
using Microsoft.EntityFrameworkCore;
using Casagres.API.Services;
using Casagres.API;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<OneDriveService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddSingleton<PipelineService>();
builder.Services.AddSingleton<PipelineEstadoService>();
builder.Services.AddHostedService<ActualizacionAutomaticaService>();
builder.Services.AddSingleton<ProductoService>();
builder.Services.AddScoped<AuthService>();
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
builder.Services.AddSingleton<MicrosoftAuthService>();
builder.Services.AddSingleton<GraphService>();

// Conexión con PostgreSQL
builder.Services.AddDbContext<CasagresDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("CadenaPostgres")
    )
);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("ReactPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();