# dockerfile generé auto par la création du projet Server, c'est en " multistep".

# below => image aspnet:8.0, juste de quoi run le .net, pas de compilateur. C'est pour la base.
# User => change l'utilisateur des commandes qui vont suivres, $APP_UID c'est une variable de l'image microsoft
# c'est un utilisateur non root, moins dangereux que de tout faire en root.
# 8080/8081 => indicatif des ports que va écouter le conteneur; c'est le docker-compose qui va va faire le mapping. 
# on installe curl en root (on a besoin de curl pour /health http).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/* 
USER $APP_UID
WORKDIR /app
EXPOSE 8080




FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TMS2000.Server/TMS2000.Server.csproj", "TMS2000.Server/"]
COPY ["TMS2000.Core/TMS2000.Core.csproj", "TMS2000.Core/"]
COPY ["TMS2000.Shared/TMS2000.Shared.csproj", "TMS2000.Shared/"]
RUN dotnet restore "TMS2000.Server/TMS2000.Server.csproj"
COPY . .
WORKDIR "/src/TMS2000.Server"
RUN dotnet build "./TMS2000.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# below : from => sdk => image complète avec le compilateur et commande dotnet. pas juste le runtime
#workdir => same principe
# copy => ajoute tout les projets dans le conteneur
# run dotenet restore => restore les nuggets necessaire
# copy . . copy le code en lui même
#workdir src => server => ce met dans le workdir du proj server
# compile le projet avec la variable build_configuration. => -o = output => permet de décider ou arrive le résultat
#de la compilation (ici /app/build). l'interet du build ici est surtout de voir si ça compile entièrement pendant la
# la création du docker, en gros ça block ici si il y'a un soucis plutot que de passer à l'étape suivante.

# les configurations de build sont pas juste des noms attribués, ils ont un impact sur le comportement à la compilation
# en Release => le compilateur optimise le code (remove de code mort and such, n'inclut pas les if DEBUG eventuel)
# en debug => file debug et .pdb et if debug actif.


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TMS2000.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# above sert à séparer le build et publish, c'est pas necessaire pour moi. En gros le publish fait un peu plus que le
# build ( le build sert pour le dev, le publish s'occupe d'optimiser pour publier, en vrai c'est là parce que le framework
# apiweb l'ajoute de base. Je peux le virer.
# le usaAppHost = false c'est pour pas installer un executif natif dans le conteneur. ici mon entrypoint specifie dotnet.
# j'ai pas besoin du coup de l'executif natif. Du coup il est en false.

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TMS2000.Server.dll"]

# récupère le produit de publish fait à l'étape précedente et la met dans /app. ensuite y'a l'entry point qui devient dispo.