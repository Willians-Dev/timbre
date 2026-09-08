using System.Text;
using System.Threading.RateLimiting;

using Azure.Monitor.OpenTelemetry.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using PdfSharp.Fonts;

using Timbre.Api.Data;
using Timbre.Api.Options;
using Timbre.Api.Services;


// =========================================================
// PDFSHARP
//
// WINDOWS:
//
// Durante desarrollo se permite utilizar las fuentes
// instaladas en Windows.
//
// LINUX / AZURE:
//
// Después de construir la aplicación se configura
// PdfFontResolver para utilizar las fuentes incluidas
// expresamente en la aplicación.
// =========================================================

if (OperatingSystem.IsWindows())
{
    GlobalFontSettings.UseWindowsFontsUnderWindows =
        true;
}


// =========================================================
// CREAR BUILDER
// =========================================================

var builder =
    WebApplication.CreateBuilder(
        args
    );


// =========================================================
// FORWARDED HEADERS
//
// Timbre.Api podrá ejecutarse detrás de:
//
// - Azure Container Apps
// - Nginx
// - otro reverse proxy
//
// Esto permite recuperar:
//
// - IP original del cliente
// - protocolo original HTTP / HTTPS
//
// IMPORTANTE:
//
// app.UseForwardedHeaders() se ejecutará posteriormente,
// después de builder.Build().
// =========================================================

builder.Services
    .Configure<ForwardedHeadersOptions>(
        options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;


            // =============================================
            // PROXY
            //
            // Permite procesar forwarded headers enviados
            // por la infraestructura de proxy.
            //
            // Antes de producción definitiva revisaremos
            // esta configuración según la topología exacta
            // de Azure.
            // =============================================

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        }
    );


// =========================================================
// CONTROLLERS
// =========================================================

builder.Services
    .AddControllers();


// =========================================================
// OPENAPI
// =========================================================

builder.Services
    .AddOpenApi();


// =========================================================
// BASE DE DATOS
// =========================================================

builder.Services
    .AddDbContext<TimbreDbContext>(
        options =>
            options.UseNpgsql(
                builder.Configuration
                    .GetConnectionString(
                        "TimbreConnection"
                    ),

                npgsqlOptions =>
                {
                    npgsqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount:
                                5,

                            maxRetryDelay:
                                TimeSpan
                                    .FromSeconds(
                                        10
                                    ),

                            errorCodesToAdd:
                                null
                        );
                }
            )
    );


// =========================================================
// SERVICIOS DE LA APLICACIÓN
// =========================================================

builder.Services
    .AddScoped<PasswordService>();

builder.Services
    .AddScoped<JwtService>();

builder.Services
    .AddScoped<AuditoriaService>();

builder.Services
    .AddScoped<UsuarioActualService>();

builder.Services
    .AddScoped<IdentificacionFacialService>();

builder.Services
    .AddScoped<MarcacionService>();

builder.Services
    .AddScoped<HistorialAsistenciaService>();

builder.Services
    .AddScoped<ReportePdfService>();


// =========================================================
// NORMALIZACIÓN DE USUARIOS
//
// Se utilizará desde:
//
// - AuthController
// - UsuariosController
//
// para normalizar:
//
// trim
// lowercase
// validación de caracteres
// =========================================================

builder.Services
    .AddScoped<UsernameService>();


// =========================================================
// FECHA / HORA
//
// Singleton porque también administra el reloj simulado
// durante Development.
// =========================================================

builder.Services
    .AddSingleton<FechaHoraService>();


// =========================================================
// CORS
// =========================================================

builder.Services
    .AddCors(
        options =>
        {
            options.AddPolicy(
                "Frontend",

                policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:4200",
                            "https://localhost:4200"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            );
        }
    );


// =========================================================
// FACE AI - OPTIONS
// =========================================================

builder.Services
    .Configure<FaceAiOptions>(
        builder.Configuration
            .GetSection(
                FaceAiOptions.SectionName
            )
    );


// =========================================================
// FACE AI - HTTP CLIENT
// =========================================================

builder.Services
    .AddHttpClient<FaceAiService>(
        (
            serviceProvider,
            client
        ) =>
        {
            var configuration =
                serviceProvider
                    .GetRequiredService<
                        IConfiguration
                    >();


            var baseUrl =
                configuration[
                    "FaceAI:BaseUrl"
                ]
                ??
                throw new
                    InvalidOperationException(
                        "FaceAI:BaseUrl no está configurado."
                    );


            var timeoutSegundos =
                configuration
                    .GetValue<int>(
                        "FaceAI:TimeoutSegundos",
                        30
                    );


            client.BaseAddress =
                new Uri(
                    baseUrl
                );


            client.Timeout =
                TimeSpan
                    .FromSeconds(
                        timeoutSegundos
                    );
        }
    );


// =========================================================
// IMPORTANTE - MULTIPART
//
// NO configuramos aquí un límite global.
//
// Razón:
//
// El kiosko envía 1 fotografía.
//
// El enrolamiento facial envía 3 fotografías.
//
// Un límite global de 6 MB podría bloquear el enrolamiento.
//
// El límite específico del kiosko se configurará en:
//
// MarcacionesFacialesController
//
// mediante:
//
// RequestSizeLimit
// RequestFormLimits
// =========================================================


// =========================================================
// RATE LIMITING
//
// Política específica:
//
// KioscoFacial
//
// 10 solicitudes por minuto por IP.
// =========================================================

builder.Services
    .AddRateLimiter(
        options =>
        {
            // =============================================
            // STATUS HTTP
            // =============================================

            options.RejectionStatusCode =
                StatusCodes
                    .Status429TooManyRequests;


            // =============================================
            // POLÍTICA KIOSKO FACIAL
            // =============================================

            options.AddPolicy(
                "KioscoFacial",

                httpContext =>
                {
                    var ip =
                        httpContext
                            .Connection
                            .RemoteIpAddress?
                            .ToString()
                        ??
                        "ip-desconocida";


                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            partitionKey:
                                ip,

                            factory:
                                _ =>
                                    new
                                    FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit =
                                            10,

                                        Window =
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                ),

                                        QueueLimit =
                                            0,

                                        QueueProcessingOrder =
                                            QueueProcessingOrder
                                                .OldestFirst,

                                        AutoReplenishment =
                                            true
                                    }
                        );
                }
            );


            // =============================================
            // RESPUESTA 429
            // =============================================

            options.OnRejected =
                async (
                    context,
                    cancellationToken
                ) =>
                {
                    context
                        .HttpContext
                        .Response
                        .ContentType =
                            "application/json";


                    await context
                        .HttpContext
                        .Response
                        .WriteAsJsonAsync(
                            new
                            {
                                mensaje =
                                    "Se han realizado demasiados intentos. " +
                                    "Espere un momento antes de volver a intentar."
                            },

                            cancellationToken
                        );
                };
        }
    );


// =========================================================
// JWT
// =========================================================

var jwtKey =
    builder.Configuration[
        "Jwt:Key"
    ]
    ??
    throw new
        InvalidOperationException(
            "Jwt:Key no está configurado."
        );


var jwtIssuer =
    builder.Configuration[
        "Jwt:Issuer"
    ];


var jwtAudience =
    builder.Configuration[
        "Jwt:Audience"
    ];


builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme
    )
    .AddJwtBearer(
        options =>
        {
            options
                .TokenValidationParameters =
                    new
                    TokenValidationParameters
                    {
                        ValidateIssuer =
                            true,

                        ValidateAudience =
                            true,

                        ValidateLifetime =
                            true,

                        ValidateIssuerSigningKey =
                            true,

                        ValidIssuer =
                            jwtIssuer,

                        ValidAudience =
                            jwtAudience,

                        IssuerSigningKey =
                            new
                            SymmetricSecurityKey(
                                Encoding
                                    .UTF8
                                    .GetBytes(
                                        jwtKey
                                    )
                            ),

                        ClockSkew =
                            TimeSpan.Zero
                    };
        }
    );


// =========================================================
// AUTORIZACIÓN
// =========================================================

builder.Services
    .AddAuthorization();


// =========================================================
// OBSERVABILIDAD
//
// Azure Monitor / Application Insights
//
// LOCAL:
//
// Si APPLICATIONINSIGHTS_CONNECTION_STRING no está
// definida, la aplicación funciona normalmente sin enviar
// telemetría.
//
// AZURE:
//
// La variable se configurará como secreto/configuración del
// Container App.
//
// Nunca debe almacenarse en Git.
// =========================================================

var applicationInsightsConnectionString =
    builder.Configuration[
        "APPLICATIONINSIGHTS_CONNECTION_STRING"
    ];


if (
    !string.IsNullOrWhiteSpace(
        applicationInsightsConnectionString
    )
)
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(
            options =>
            {
                options.ConnectionString =
                    applicationInsightsConnectionString;
            }
        );
}


// =========================================================
// CREAR APLICACIÓN
// =========================================================

var app =
    builder.Build();


// =========================================================
// PDFSHARP - LINUX / AZURE
//
// En Windows ya utilizamos las fuentes de Windows.
//
// En Linux / Azure utilizaremos nuestro FontResolver.
// =========================================================

if (!OperatingSystem.IsWindows())
{
    GlobalFontSettings.FontResolver =
        new PdfFontResolver(
            app.Environment
        );
}


// =========================================================
// OPENAPI SOLO DEVELOPMENT
// =========================================================

if (
    app.Environment
        .IsDevelopment()
)
{
    app.MapOpenApi();
}


// =========================================================
// FORWARDED HEADERS
//
// DEBE ejecutarse antes de:
//
// - CORS
// - Rate Limiter
// - Authentication
// - Authorization
//
// especialmente porque Rate Limiter utiliza:
//
// HttpContext.Connection.RemoteIpAddress
// =========================================================

app.UseForwardedHeaders();


// =========================================================
// HTTPS
//
// DESARROLLO:
//
// Kestrel se mantiene actualmente por HTTP.
//
// AZURE:
//
// Azure Container Apps terminará TLS en el ingress.
//
// Configuraremos:
//
// External ingress
// Allow insecure = false
//
// Por tanto, no activamos aquí redirección HTTPS para
// desarrollo local.
// =========================================================

// app.UseHttpsRedirection();


// =========================================================
// CORS
// =========================================================

app.UseCors(
    "Frontend"
);


// =========================================================
// RATE LIMITING
// =========================================================

app.UseRateLimiter();


// =========================================================
// AUTENTICACIÓN
// =========================================================

app.UseAuthentication();


// =========================================================
// AUTORIZACIÓN
// =========================================================

app.UseAuthorization();


// =========================================================
// CONTROLADORES
// =========================================================

app.MapControllers();


// =========================================================
// EJECUTAR
// =========================================================

app.Run();