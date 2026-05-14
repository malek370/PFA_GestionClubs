# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur de débogage et comment Visual Studio utilise ce Dockerfile pour générer vos images afin d’accélérer le débogage.

# Cet index est utilisé lors de l’exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Cette phase est utilisée pour générer le projet de service
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GestionClubs/GestionClubs.API.csproj", "GestionClubs/"]
COPY ["Application/GestionClubs.Application.csproj", "Application/"]
COPY ["Infrastructure/GestionClubs.Infrastructure.csproj", "Infrastructure/"]
COPY ["Domain/GestionClubs.Domain.csproj", "Domain/"]
RUN dotnet restore "./GestionClubs/GestionClubs.API.csproj"
COPY . .
WORKDIR "/src/GestionClubs"
RUN dotnet build "./GestionClubs.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Cette étape permet de publier le projet de service à copier dans la phase finale
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GestionClubs.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#phase finale pour exécuter l'application
# Cette phase est utilisée en production ou lors de l’exécution à partir de VS en mode normal (par défaut quand la configuration de débogage n’est pas utilisée)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY entrypoint.sh /app/entrypoint.sh
# Run as root so update-ca-certificates can trust the mounted self-signed cert
USER root
RUN chmod +x /app/entrypoint.sh
ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]