# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# Copy solution and project file first for layer-cached restore
COPY code-metric-collector.sln ./
COPY src/CodeMetricCollector.csproj src/

RUN dotnet restore src/CodeMetricCollector.csproj

# Copy the rest of the source and publish
COPY src/ src/

RUN dotnet publish src/CodeMetricCollector.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "CodeMetricCollector.dll"]
