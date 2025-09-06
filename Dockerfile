# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY src/SurlesMobile.Api.csproj ./src/
RUN dotnet restore src/SurlesMobile.Api.csproj

# Copy source code and build
COPY src/ ./src/
RUN dotnet publish src/SurlesMobile.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory and create logs directory
WORKDIR /app
RUN mkdir -p logs && chown -R appuser:appuser /app

# Copy published app
COPY --from=build --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose port
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "SurlesMobile.Api.dll"]
