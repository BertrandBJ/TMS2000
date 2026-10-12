using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TMS2000.Core.Repositories;
using TMS2000.Core.Data;
using TMS2000.Core.Services;
using TMS2000.Server.Middleware;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using TMS2000.Server.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);
MongoConventions.Register(); // call les regles de C# <=> mongoDB.


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

#region Configuration

var mongoConnectionString = Required(builder.Configuration, "ConnectionStrings:MongoDb");
var mongoDatabaseName = Required(builder.Configuration, "MongoSettings:DatabaseName");
var jwtSecret = Required(builder.Configuration, "Jwt:Secret");
var jwtExpiryHours = int.Parse(Required(builder.Configuration, "Jwt:ExpiryHours"));
var jwtRefreshTokenDays = int.Parse(Required(builder.Configuration, "Jwt:RefreshTokenDays"));



#endregion 

#region Services
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>     // fait passer le token dans les request swagger, sinon les routes sont opens pour tous//
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Colle uniquement l'accessToken renvoyé par /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));// singleton => une seul instance pour toutes la vie du programme. also c'est là qu'est la connectionstring à mangoDB qui provient du .env.
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName)); // bcp plus easy que de faire un scope avec les variables inside.

builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped(sp =>
    new AuthService(sp.GetRequiredService<UserRepository>(), jwtSecret, jwtExpiryHours, jwtRefreshTokenDays));

var priceTickSeconds = int.Parse(Required(builder.Configuration, "PriceEngine:TickIntervalSeconds"));

builder.Services.AddScoped<ResourceRepository>();

builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<PriceEngineService>();
builder.Services.AddScoped<RuleService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddSingleton<MarketClock>();
builder.Services.AddHostedService(sp => new PriceTickService(
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<MarketClock>(),
    sp.GetRequiredService<ILogger<PriceTickService>>(),
    TimeSpan.FromSeconds(priceTickSeconds)));


// above : j'add à mon healthcheck un healthcheck interne à la bdd. pour ce faire il execute une fonction sp =>
// par convention c'est sp pour IServiceProvider => imposé dans l'écriture de addMongoDb, il est possible de faire 
// d'accès à mongodb plus général (ou je vais faire sp => GetRequiredService).
#endregion

#region Auth


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, // fonction de base de la validation par token => dit au backend : je veux que tu vérifier le token.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), // ou est la clé secrete
            ValidateIssuer = false, // je ne vérifie pas qui send le token.
            ValidateAudience = false,
            ValidateLifetime = true // refuse les tokens timed-out
        };
    });

builder.Services.AddAuthorization();

#endregion

#region Middleware
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseStatusCodePages();          
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // précision ici => mapcontrollers doit être après auth et author. sinon les 2 middlesware d'auth n'ont jamais l'occassion de run pendant une request.
app.MapHealthChecks("/health").AllowAnonymous(); // allow healthcheck sans besoin d'auth.


//below : appel mes indexes mongoDB.
var mongoClient = app.Services.GetRequiredService<IMongoClient>(); // Singleton, pas besoin de scope
await MongoIndexes.EnsureAllAsync(mongoClient.GetDatabase(mongoDatabaseName)); // index créés avant la 1re requête

#endregion
 
app.Run();

static string Required(IConfiguration config, string key) =>
    string.IsNullOrWhiteSpace(config[key])
        ? throw new InvalidOperationException($"La configuration '{key}' est manquante ou vide.")
        : config[key]!;
