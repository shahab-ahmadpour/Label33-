# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Label33.sln ./
COPY src/Label33.Domain/Label33.Domain.csproj src/Label33.Domain/
COPY src/Label33.Application/Label33.Application.csproj src/Label33.Application/
COPY src/Label33.Infrastructure/Label33.Infrastructure.csproj src/Label33.Infrastructure/
COPY src/Label33.Web/Label33.Web.csproj src/Label33.Web/
COPY tests/Label33.Tests/Label33.Tests.csproj tests/Label33.Tests/

RUN dotnet restore Label33.sln

COPY . .
RUN dotnet publish src/Label33.Web/Label33.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Writable folder for SQLite demo DB + uploads
RUN mkdir -p /app/App_Data && chmod -R 777 /app/App_Data

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Provider=Sqlite
ENV ConnectionStrings__DefaultConnection="Data Source=/app/App_Data/label33.db"
ENV Storage__Root=/app/App_Data/files

COPY --from=build /app/publish .
EXPOSE 8080

CMD ["dotnet", "Label33.Web.dll"]
