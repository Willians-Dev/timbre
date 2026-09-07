using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Timbre.Api.Data;
using Timbre.Api.Services;
using Timbre.Api.Options;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// CONTROLLERS
// =====================================================
builder.Services.AddControllers();

// =====================================================
// OPENAPI
// =====================================================
builder.Services.AddOpenApi();

// =====================================================
// BASE DE DATOS
// =====================================================
builder.Services.AddDbContext<TimbreDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "TimbreConnection"
        ),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
        }
    )
);

// =====================================================
// SERVICIOS DE LA APLICACIÓN
// =====================================================
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<UsuarioActualService>();
builder.Services.AddScoped<IdentificacionFacialService>();
builder.Services.AddScoped<MarcacionService>();

// FechaHoraService se mantiene Singleton porque también
// administra el reloj simulado durante Development.
builder.Services.AddSingleton<FechaHoraService>();

// =====================================================
// CORS
// =====================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<FaceAiOptions>(
    builder.Configuration.GetSection(
        FaceAiOptions.SectionName
    )
);

builder.Services.AddHttpClient<FaceAiService>(
    (serviceProvider, client) =>
    {
        var configuration =
            serviceProvider
                .GetRequiredService<IConfiguration>();

        var baseUrl =
            configuration["FaceAI:BaseUrl"]
            ?? throw new InvalidOperationException(
                "FaceAI:BaseUrl no está configurado."
            );

        var timeoutSegundos =
            configuration.GetValue<int>(
                "FaceAI:TimeoutSegundos",
                30
            );

        client.BaseAddress =
            new Uri(baseUrl);

        client.Timeout =
            TimeSpan.FromSeconds(
                timeoutSegundos
            );
    }
);

// =====================================================
// JWT
// =====================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key no está configurado."
    );

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

// =====================================================
// AUTORIZACIÓN
// =====================================================
builder.Services.AddAuthorization();

// =====================================================
// CREAR APLICACIÓN
// =====================================================
var app = builder.Build();

// =====================================================
// OPENAPI SOLO DEVELOPMENT
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// =====================================================
// HTTPS
// =====================================================
// Por ahora se mantiene deshabilitado porque
// estamos ejecutando localmente por HTTP.
// app.UseHttpsRedirection();

// =====================================================
// CORS
//
// Debe ejecutarse antes de Authentication / Authorization.
// =====================================================
app.UseCors("Frontend");

// =====================================================
// SEGURIDAD
// =====================================================
app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// CONTROLADORES
// =====================================================
app.MapControllers();

// =====================================================
// EJECUTAR
// =====================================================
app.Run();