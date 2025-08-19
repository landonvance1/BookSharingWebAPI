# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 Web API project for a book sharing application built using ASP.NET Core Minimal APIs. The application provides REST endpoints for managing books with in-memory storage using a mock database.

## Development Commands

### Building and Running
- `dotnet build BookSharingApp.csproj` - Build the application (use specific project file due to .sln presence)
- `dotnet run` - Run the application (starts on https://localhost:7061 and http://localhost:5155)
- `dotnet run --launch-profile http` - Run with HTTP only
- `dotnet run --launch-profile https` - Run with HTTPS (default)

### Testing
- No test framework is currently configured in this project

### Package Management
- `dotnet restore` - Restore NuGet packages
- `dotnet add package <PackageName>` - Add a new package

### Docker Commands

#### First Time Setup
- `cp .env.example .env` - Copy environment template (required for local development)
- Edit `.env` file with your preferred database credentials

#### Running the Application
- `docker build -t booksharing-api .` - Build Docker image
- `docker run -p 3000:8080 booksharing-api` - Run container on port 3000
- `docker-compose --profile prod up` - Start with docker-compose (production mode, port 3000)
- `docker-compose --profile dev up` - Start with docker-compose (development mode, port 3001)
- `docker-compose down` - Stop and remove containers
- `docker-compose up --build` - Rebuild and start containers

## Architecture

### Project Structure
- **Program.cs** - Application entry point and configuration
- **Models/** - Data models (Book entity)
- **Endpoints/** - API endpoint definitions using extension methods
- **Data/** - Data access layer with mock database implementation

### Key Components

1. **Minimal API Architecture**: Uses ASP.NET Core Minimal APIs with static extension methods for endpoint mapping
2. **Dependency Injection**: MockDatabase is registered as a singleton service
3. **In-Memory Storage**: MockDatabase class provides CRUD operations with seed data
4. **Swagger Integration**: Configured for development environment with OpenAPI documentation

### Data Flow
- Endpoints are mapped in `BookEndpoints.MapBookEndpoints()` extension method
- MockDatabase singleton handles all data operations
- Book model is a simple POCO with Id, Title, Author, and ISBN properties

### Available Endpoints
- GET `/books` - Get all books
- GET `/books/{id}` - Get book by ID
- POST `/books` - Add new book
- GET `/books/search?title=&author=` - Search books by title and/or author

## Configuration
- **appsettings.json** - Production configuration
- **appsettings.Development.json** - Development-specific settings
- **launchSettings.json** - Development server profiles and URLs

## Docker Deployment

### Container Configuration
- **Port 8080** - Internal container port (HTTP)
- **Port 3000** - External mapped port for production
- **Port 3001** - External mapped port for development mode
- Static files (wwwroot) are volume-mounted for easy updates

### Docker Files
- **Dockerfile** - Multi-stage build configuration using .NET 8 SDK and runtime
- **.dockerignore** - Excludes build artifacts and unnecessary files from Docker context
- **docker-compose.yml** - Container orchestration with production and development profiles

### Prerequisites
- Docker Desktop for Windows must be installed to build and run containers
- Create `.env` file from `.env.example` template for local development

### Environment Variables
The application uses environment variables for database configuration to keep credentials secure:
- **POSTGRES_DB** - Database name
- **POSTGRES_USER** - Database username  
- **POSTGRES_PASSWORD** - Database password
- **DB_CONNECTION_STRING** - Full connection string for the application

For local development, copy `.env.example` to `.env` and customize the values as needed.