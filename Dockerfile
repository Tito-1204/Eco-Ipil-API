# Usar uma imagem base compatível com .NET 6
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Etapa de construção
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EcoIpil.API.csproj", "./"]
RUN dotnet restore

COPY . .
WORKDIR "/src/"
RUN dotnet build --configuration Release --no-restore
RUN dotnet publish --configuration Release --no-restore -o /app/publish

# Etapa final: Configurar a execução
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcoIpil.API.dll"]