using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc; // problemDetails viens de là
using TMS2000.Core.Exceptions;
namespace TMS2000.Server.Middleware;

public class GlobalExceptionHandler : IExceptionHandler //interface qui promet la methode TryHandleAsync. ça veut dire que chaque classe qui implémente IExceptionHandler possède un TryHandleAsync.
{
    private readonly ILogger<GlobalExceptionHandler> _logger; // Ilogger service de base de .net <GlobalExceptionHandler> va ajouter l'origine du log.

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        
        HttpContext httpContext, // type de request http.
        Exception exception, // l'erreur qui viens d'être catch
        CancellationToken cancellationToken) // token de cancellation de l'app au besoin, on le transmet au reste de l'application pour tout fermer.
    {
        
        var (statusCode, message) = exception switch 
            
            // ici on déclare 2 variables, comme si c'était un tuple mais là on fait 2 variables avec 1 valeurs chaque plutot que 1 variable avec 2 valeurs in.
            // btw => exception switch c'est comme switch(exception). le switch va tester l'exception qu'il a reçu. (exception est déclaré plus haut btw).
        {
            
            AppException appException => (appException.StatusCode, appException.Message),
            _ => (StatusCodes.Status500InternalServerError, "Une erreur interne est survenue.") 
            
            // s'active de base quand une erreur arrive mais qu'il n'y a pas de type d'erreur spécifique. (en gros c'est l'erreur 500).
        };

        if (exception is AppException)
        {
            _logger.LogWarning("{StatusCode} {Message}",statusCode,message);
        }
        else
        {
            _logger.LogError(exception, "Une erreur imprévue est survenue sur {path}", httpContext.Request.Path);
        }
        
        // logging de l'erreur, dans les () ce qu'il va écrire, après le switch.

        var isDevelopment = httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment(); 
            
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Detail = isDevelopment && exception is not AppException ?
                // on ajoute tout le details de l'environnement web, seulement si on est en Dev
                 exception.ToString()
                : null
        };
        
        httpContext.Response.StatusCode = statusCode; 
        // necessaire pour formater la réponse en suivant les conventions HTTP, c'est pas juste cosmétique ça correspond à une donnée structurelle du protocol HTTP. (c'est dans le header de la réponse).
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken); 
        // ah ça c'est la réponse qui va être send à celui qui demande la requête, exemple postman., c'est dans le body de la réponse. 

        return true; // means => ok j'ai réussi à gérer l'erreur et j'ai envoyer la réponse sous format json à l'utilisateur. 
    }
}