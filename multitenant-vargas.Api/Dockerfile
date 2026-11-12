FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Desactiva telemetría y procesos en segundo plano para ahorrar RAM en Render
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_XMLDOC_MODE=skip \
    MSBUILDDISABLENODEREUSE=1

# Copia los archivos y restaura dependencias primero
COPY . .
RUN dotnet restore -m:1

# Publica usando 1 solo hilo y sin generar ejecutable nativo extra
RUN dotnet publish -c Release -o /app/publish --no-restore -m:1 /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "multitenant-vargas.Api.dll"]
