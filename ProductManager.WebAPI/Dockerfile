# syntax=docker/dockerfile:1

# ---- Build & publish the API ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first so `dotnet restore` is cached
# unless a .csproj actually changes.
COPY ["ProductManager.Domain/ProductManager.Domain.csproj", "ProductManager.Domain/"]
COPY ["ProductManager.Application/ProductManager.Application.csproj", "ProductManager.Application/"]
COPY ["ProductManager.Infrastructure/ProductManager.Infrastructure.csproj", "ProductManager.Infrastructure/"]
COPY ["ProductManager.Presentation/ProductManager.Presentation.csproj", "ProductManager.Presentation/"]
COPY ["ProductManager.WebAPI/ProductManager.WebAPI.csproj", "ProductManager.WebAPI/"]

RUN dotnet restore "ProductManager.WebAPI/ProductManager.WebAPI.csproj"

# Now copy the rest of the source and publish
COPY ProductManager.Domain/. ProductManager.Domain/
COPY ProductManager.Application/. ProductManager.Application/
COPY ProductManager.Infrastructure/. ProductManager.Infrastructure/
COPY ProductManager.Presentation/. ProductManager.Presentation/
COPY ProductManager.WebAPI/. ProductManager.WebAPI/

RUN dotnet publish "ProductManager.WebAPI/ProductManager.WebAPI.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# EF Core migrations + seed data behavior is controlled by the Database:ApplyMigrationsOnStartup
# and Database:SeedSampleData settings (see DatabaseMigrator / DatabaseSeeder, invoked from Program.cs).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductManager.WebAPI.dll"]
