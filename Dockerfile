# syntax=docker/dockerfile:1
# Multi-stage build for ASP.NET Core 8 API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first for layer caching
COPY Nairabox.sln .
COPY src/Nairabox.Api/Nairabox.Api.csproj src/Nairabox.Api/
COPY src/Nairabox.Domain/Nairabox.Domain.csproj src/Nairabox.Domain/
COPY src/Nairabox.Application/Nairabox.Application.csproj src/Nairabox.Application/
COPY src/Nairabox.Infrastructure/Nairabox.Infrastructure.csproj src/Nairabox.Infrastructure/
RUN dotnet restore src/Nairabox.Api/Nairabox.Api.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish src/Nairabox.Api/Nairabox.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime image — slim, non-root
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Run as non-root for defense in depth
RUN useradd --uid 1000 --create-home --shell /bin/bash app && chown -R app:app /app
USER app

# Use shell-form so $PORT injected by Render/Fly/Heroku is actually expanded
# at runtime. Defaults to 8080 if the host doesn't set one (Fly + local).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Nairabox.Api.dll"]
