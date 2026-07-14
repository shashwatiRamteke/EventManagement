# Event Management API

A comprehensive RESTful API for managing events, tickets, and tier-based seating categories. Built with ASP.NET Core 10.0 and Entity Framework Core with in-memory database support for development.

## Features

- 🎫 **Event Management**: Create and manage events with venue, date, and time details
- 🎟️ **Ticket System**: Purchase tickets with tier-based pricing and categories
- 🔒 **Inventory Control**: Prevent overselling with hold/reservation pattern and concurrency management
- 📊 **Tier Categories**: Flexible seating options (VIP, Premium, Standard, etc.)
- 🏥 **Health Checks**: Built-in health and readiness endpoints for production monitoring
- 📖 **API Documentation**: Interactive API documentation with Scalar UI
- 🐳 **Docker Support**: Containerized deployment with included Dockerfile

## Prerequisites

Before setting up this API locally, ensure you have the following installed:

- **.NET 10.0 SDK** or later
  - [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)
  - Verify installation: `dotnet --version`

- **Visual Studio 2026** (recommended) or **Visual Studio Code**
  - Visual Studio 2026 Community, Professional, or Enterprise
  - OR Visual Studio Code with C# Dev Kit extension

- **Git** (for cloning the repository)
  - [Download Git](https://git-scm.com/downloads)

- **Docker Desktop** (optional, for containerized deployment)
  - [Download Docker Desktop](https://www.docker.com/products/docker-desktop)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/shashwatiRamteke/EventManagement.git
cd EventManagement
```

### 2. Navigate to the Project Directory

```bash
cd EventManagementApi2/EventManagementApi2
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Project

```bash
dotnet build
```

### 5. Run the API

#### Option A: Using .NET CLI

```bash
dotnet run
```

The API will start on:
- HTTP: `http://localhost:5114`
- HTTPS: `https://localhost:7055`

#### Option B: Using Visual Studio 2026

1. Open the solution file: `EventManagementApi2.Tests.slnx`
2. Set `EventManagementApi2` as the startup project
3. Press `F5` or click the "Run" button
4. Choose the desired profile:
   - **http**: HTTP only on port 5114
   - **https**: HTTPS on 7055 and HTTP on 5114
   - **Container (Dockerfile)**: Run in Docker container

#### Option C: Using Visual Studio Code

1. Open the project folder in VS Code
2. Press `F5` or use the Run and Debug panel
3. Select `.NET Core Launch (web)` configuration

## Accessing the API

### API Base URLs

- **Development HTTP**: `http://localhost:5114`
- **Development HTTPS**: `https://localhost:7055`

### Interactive API Documentation

Once the API is running, access the interactive Scalar API documentation:

```
https://localhost:7055/scalar/v1
```

Or for HTTP:

```
http://localhost:5114/scalar/v1
```

### Health Check Endpoints

- **Health**: `GET /health` - Returns API health status
- **Readiness**: `GET /ready` - Returns API readiness status

## API Endpoints

### Events

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/events` | Get all events with details |
| GET | `/api/events/{id}` | Get a specific event by ID |
| POST | `/api/events` | Create a new event |

### Tickets

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/tickets/purchase` | Purchase tickets for an event |

## Sample API Requests

### Create an Event

```http
POST https://localhost:7055/api/events
Content-Type: application/json

{
  "name": "Summer Music Festival",
  "description": "Annual outdoor music festival",
  "venue": "Central Park Amphitheater",
  "date": "2025-07-15",
  "time": "18:00:00",
  "totalTicketing": 1000,
  "tierId": 1,
  "tierCategoryIds": [1, 2, 3]
}
```

### Get All Events

```http
GET https://localhost:7055/api/events
```

### Purchase Tickets

```http
POST https://localhost:7055/api/tickets/purchase
Content-Type: application/json

{
  "eventId": 1,
  "categoryId": 1,
  "quantity": 2,
  "buyerName": "John Doe",
  "buyerEmail": "john.doe@example.com"
}
```

## Project Structure

```
EventManagementApi2/
├── Controllers/           # API Controllers
│   ├── EventsController.cs
│   └── TicketsController.cs
├── Data/                  # Data access layer
│   ├── EventContext.cs    # EF Core DbContext
│   ├── EventSeeder.cs     # Database seeding
│   ├── IUnitOfWork.cs     # Unit of Work pattern
│   ├── UnitOfWork.cs
│   └── Repositories/      # Repository pattern
├── Models/                # Domain models and DTOs
│   ├── Event.cs
│   ├── Ticket.cs
│   ├── TierCategory.cs
│   └── Request/Response models
├── Services/              # Business logic services
│   ├── IInventoryService.cs
│   ├── InMemoryInventoryService.cs
│   └── HoldExpiryService.cs
├── Program.cs             # Application entry point
└── appsettings.json       # Configuration
```

## Configuration

### Database

The API uses **Entity Framework Core In-Memory Database** for development, which means:

- ✅ No database setup required
- ✅ Data is seeded automatically on startup
- ⚠️ Data is lost when the application stops
- 💡 Perfect for development and testing

To use a persistent database (SQL Server, PostgreSQL, etc.), update the `Program.cs`:

```csharp
// Replace in-memory database with a real database
builder.Services.AddDbContext<EventContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

And add the connection string to `appsettings.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost;Database=EventManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### CORS Policy

The API is configured with a permissive CORS policy for development:

```csharp
AllowAnyOrigin()
AllowAnyMethod()
AllowAnyHeader()
```

⚠️ **For production**, restrict CORS to specific origins in `Program.cs`.

### Environment Variables

Configure environment-specific settings in `appsettings.Development.json` or `appsettings.json`.

## Docker Deployment

### Build Docker Image

```bash
docker build -t eventmanagement-api -f EventManagementApi2/EventManagementApi2/Dockerfile .
```

### Run Docker Container

```bash
docker run -d -p 8080:8080 -p 8081:8081 --name eventmanagement-api eventmanagement-api
```

Access the API at:
- HTTP: `http://localhost:8080`
- HTTPS: `https://localhost:8081`

### Stop and Remove Container

```bash
docker stop eventmanagement-api
docker rm eventmanagement-api
```

## Development Features

### User Secrets

The project is configured to use User Secrets (ID: `474db4e2-6e7b-4c1e-89db-4838ab16560a`).

To manage secrets:

```bash
dotnet user-secrets set "SecretKey" "SecretValue"
dotnet user-secrets list
```

### Seeded Data

The API automatically seeds sample data on startup, including:

- Sample events with various tiers and categories
- Tier definitions (Standard, Premium, VIP)
- Category options within each tier

### Background Services

- **HoldExpiryService**: Automatically expires ticket holds/reservations to prevent abandoned reservations from blocking inventory

## Troubleshooting

### Port Already in Use

If ports 5114 or 7055 are already in use, update `launchSettings.json`:

```json
"applicationUrl": "https://localhost:YOUR_HTTPS_PORT;http://localhost:YOUR_HTTP_PORT"
```

### Certificate Issues (HTTPS)

Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

### Build Errors

Clean and rebuild:

```bash
dotnet clean
dotnet build
```

### API Not Starting

Check the logs in the console output for detailed error messages.

## Architecture & Design Patterns

- **Repository Pattern**: Abstraction over data access
- **Unit of Work Pattern**: Manages transactions across repositories
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **SOLID Principles**: Clean architecture and separation of concerns
- **Async/Await**: Non-blocking I/O operations throughout

## NuGet Packages

Key dependencies:

- `Microsoft.AspNetCore.OpenApi` (10.0.9) - OpenAPI support
- `Microsoft.EntityFrameworkCore` (10.0.9) - ORM
- `Microsoft.EntityFrameworkCore.InMemory` (10.0.9) - In-memory database
- `Scalar.AspNetCore` (2.16.11) - API documentation UI
- `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (1.23.0) - Docker support

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues, questions, or contributions, please visit:
[GitHub Repository](https://github.com/shashwatiRamteke/EventManagement)

---

**Built with ❤️ using ASP.NET Core 10.0**
