FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TMS2000.Console/TMS2000.Console.csproj", "TMS2000.Console/"]
COPY ["TMS2000.Core/TMS2000.Core.csproj", "TMS2000.Core/"]
RUN dotnet restore "TMS2000.Console/TMS2000.Console.csproj"
COPY . .
WORKDIR "/src/TMS2000.Console"
RUN dotnet build "./TMS2000.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TMS2000.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TMS2000.Console.dll"]
