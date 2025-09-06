# SurlesMobile API

A comprehensive .NET 8 Web API for portfolio management with JWT authentication, RBAC authorization, and AWS integration.

## Features

- **Entity Framework Core** with PostgreSQL support
- **JWT Authentication** with AWS Cognito integration
- **RBAC Authorization** with admin/editor/viewer roles
- **Comprehensive CRUD operations** for portfolios, sections, and items
- **Health checks** for database and application status
- **Swagger/OpenAPI** documentation
- **CORS support** for Angular frontend
- **Global error handling** middleware
- **Structured logging** with Serilog
- **AWS Secrets Manager** integration for configuration
- **Docker support** with multi-stage build and non-root user

## Architecture

### Entities
- **Site**: Portfolio sites with metadata and configuration
- **Section**: Grouped content sections (e.g., "Experience", "Projects")
- **Item**: Individual items within sections
- **Cta**: Call-to-action buttons for sites

### Authentication & Authorization
- JWT tokens from AWS Cognito
- Role-based access control:
  - **Viewer**: Read access to portfolio data
  - **Editor**: Read/write access to content
  - **Admin**: Full access including delete operations

### API Endpoints

#### Portfolio
- `GET /api/portfolio` - Get complete portfolio data
- `GET /api/portfolio/sites/{slug}` - Get site by slug
- `PUT /api/portfolio/sites/{slug}` - Update site information

#### Sections
- `GET /api/sections` - List all sections
- `GET /api/sections/{id}` - Get section by ID
- `POST /api/sections` - Create new section
- `PUT /api/sections/{id}` - Update section
- `DELETE /api/sections/{id}` - Delete section

#### Items
- `GET /api/items` - List all items
- `GET /api/items/{id}` - Get item by ID
- `POST /api/items` - Create new item
- `PUT /api/items/{id}` - Update item
- `DELETE /api/items/{id}` - Delete item

#### Health Checks
- `GET /health` - Basic health check
- `GET /health/ready` - Readiness check
- `GET /health/live` - Liveness check

## Environment Variables

### Required
```
COGNITO__ISSUER - AWS Cognito issuer URL
COGNITO__AUDIENCE - AWS Cognito audience/client ID
ConnectionStrings__Default - PostgreSQL connection string (or via AWS Secrets Manager)
```

### Optional
```
ASPNETCORE_ENVIRONMENT - Environment (Development/Production)
AWS_REGION - AWS region for Secrets Manager
```

## Development Setup

1. **Prerequisites**
   - .NET 8.0 SDK
   - PostgreSQL database
   - AWS CLI configured (for Secrets Manager)

2. **Database Setup**
   ```bash
   # Create database
   createdb portfolio_dev
   
   # Apply migrations (done automatically on startup)
   dotnet ef database update
   ```

3. **Run Locally**
   ```bash
   cd src
   dotnet run
   ```

4. **Access API**
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

## Docker Deployment

### Build Image
```bash
docker build -t surlesmobile-api .
```

### Run Container
```bash
docker run -p 80:80 \
  -e COGNITO__ISSUER=your-cognito-issuer \
  -e COGNITO__AUDIENCE=your-cognito-audience \
  -e ConnectionStrings__Default=your-connection-string \
  surlesmobile-api
```

## Configuration

### CORS
Configure allowed origins in `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://yourdomain.com"
    ]
  }
}
```

### Logging
Structured logging with Serilog to console and files:
- Development: Debug level
- Production: Information level
- Log files: `logs/api-{date}.log`

## Security Features

- Non-root Docker user
- JWT token validation
- CORS protection
- Input validation
- Global exception handling
- Secure headers

## Monitoring

- Health check endpoints for Kubernetes/AWS
- Structured logging for observability
- Application metrics via health checks

## Database Migrations

Migrations are applied automatically on startup. For manual migration:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```
