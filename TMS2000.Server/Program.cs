using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TMS2000.Core.Repositories;
using TMS2000.Core.Services;
using TMS2000.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

#region Configuration & Services

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
                            ?? throw new InvalidOperationException("La chaîne de connexion 'MongoDb' est manquante. Vérifie la variable d'environnement ConnectionStrings__MongoDb.");

var jwtExpiryHours = int.Parse(builder.Configuration["Jwt:ExpiryHours"]
                               ?? throw new InvalidOperationException("La configuration 'Jwt:ExpiryHours' est manquante."));

var jwtSecret = builder.Configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("La configuration 'Jwt:Secret' est manquante. Vérifie la variable d'environnement Jwt__Secret.");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString)); // singleton => une seul instance pour toutes la vie du programme. also c'est là qu'est la connectionstring à mangoDB qui provient du .env.
builder.Services.AddScoped(sp => // scoped => une nouvelle instance par request http.
{
    var client = sp.GetRequiredService<IMongoClient>(); // chaque fois qu'un service à besoin de mongoDB va chercher IMongoClient qu'on a df plus haut, celui avec la connection string.
    
    var databaseName = builder.Configuration["MongoSettings:DatabaseName"]// ah et c'est là le mangosettings__ de dockerfile. d'accord... va chercher le nom de la database dans le .env.
                       ?? throw new InvalidOperationException("La configuration 'MongoSettings:DatabaseName' est manquante."); // erreur if no name de la database.
    return client.GetDatabase(databaseName);
});
builder.Services.AddHealthChecks()
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped(sp =>
{
    var userRepository = sp.GetRequiredService<UserRepository>();
    return new AuthService(userRepository, jwtSecret, jwtExpiryHours);
});
// above : j'add à mon healthcheck un healthcheck interne à la bdd. pour ce faire il execute une fonction sp =>
// par convention c'est sp pour IServiceProvider => imposé dans l'écriture de addMongoDb, il est possible de faire 
// _ => new etc. J'utilise pas le sp pour le moment, mais je vais l'utiliser plus tard quand je vais build un outil
// d'accès à mongodb plus général (ou je vais faire sp => GetRequiredService).
#endregion

#region Auth


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
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
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers(); // précision ici => mapcontrollers doit être après auth et author. sinon les 2 middlesware d'auth n'ont jamais l'occassion de run pendant une request.
app.MapHealthChecks("/health").AllowAnonymous(); // allow healthcheck sans besoin d'auth.
#endregion

app.Run();
