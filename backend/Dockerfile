FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RetoAbierto.sln .
COPY RetoAbierto.Domain/RetoAbierto.Domain.csproj RetoAbierto.Domain/
COPY RetoAbierto.Application/RetoAbierto.Application.csproj RetoAbierto.Application/
COPY RetoAbierto.Infrastructure/RetoAbierto.Infrastructure.csproj RetoAbierto.Infrastructure/
COPY RetoAbierto.API/RetoAbierto.API.csproj RetoAbierto.API/
RUN dotnet restore RetoAbierto.API/RetoAbierto.API.csproj

COPY . .
RUN dotnet publish RetoAbierto.API/RetoAbierto.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RetoAbierto.API.dll"]