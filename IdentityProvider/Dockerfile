# Consultez https://aka.ms/customizecontainer pour savoir comment personnaliser votre conteneur de débogage et comment Visual Studio utilise ce Dockerfile pour générer vos images afin d'accélérer le débogage.

# Cet index est utilisé lors de l'exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Cette phase est utilisée pour générer le projet de service
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IdentityProvider.csproj", "."]
RUN dotnet restore "./IdentityProvider.csproj"
COPY Abstracts/ ./Abstracts/
COPY Controllers/ ./Controllers/
COPY DbContext/ ./DbContext/
COPY Entities/ ./Entities/
COPY Exceptions/ ./Exceptions/
COPY Handlers/ ./Handlers/
COPY Options/ ./Options/
COPY Repositories/ ./Repositories/
COPY Requests/ ./Requests/
COPY Migrations/ ./Migrations/
COPY Processors/ ./Processors/
COPY Services/ ./Services/
COPY appsettings.json ./
COPY Program.cs ./
COPY Validators/ ./Validators/
COPY Migrations/ ./Migrations/
COPY Responses/ ./Responses/
WORKDIR "/src/."
RUN dotnet build "IdentityProvider.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Cette étape permet de publier le projet de service à copier dans la phase finale
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IdentityProvider.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Cette phase est utilisée en production ou lors de l'exécution à partir de VS en mode normal (par défaut quand la configuration de débogage n'est pas utilisée)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
RUN apt-get update && apt-get install -y --no-install-recommends openssl && rm -rf /var/lib/apt/lists/* && mkdir -p /app/keys && chown -R $APP_UID /app/keys
USER $APP_UID
ENTRYPOINT ["sh", "-c", "if [ ! -f /app/keys/private_key.pem ]; then openssl genrsa -out /app/keys/private_key.pem 2048 && openssl rsa -in /app/keys/private_key.pem -pubout -outform PEM | openssl rsa -pubin -RSAPublicKey_out -out /app/keys/public_key.pem; fi && ln -sf /app/keys/private_key.pem /app/private_key.pem && ln -sf /app/keys/public_key.pem /app/public_key.pem && exec dotnet IdentityProvider.dll"]


# Commandes pour construire et exécuter le conteneur
# docker build -t identityprovider .
# docker run -p 8080:8080 \
#   -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=IdentityProvider;User Id=sa;Password=YourPass;TrustServerCertificate=True;" \
#   -e JwtOptions__Secret="votre-secret-de-production-suffisamment-long" \
#   -e JwtOptions__Issuer="myapp" \
#   -e JwtOptions__Audience="myappusers" \
#   identityprovider
