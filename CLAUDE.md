# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 Web API project for a book sharing application built using ASP.NET Core Minimal APIs. The application provides REST endpoints for managing books with in-memory storage using a mock database.

## Development Commands

### Building and Running
- `dotnet build` - Build the application
- `dotnet run` - Run the application (starts on https://localhost:7061 and http://localhost:5155)
- `dotnet run --launch-profile http` - Run with HTTP only
- `dotnet run --launch-profile https` - Run with HTTPS (default)

### Testing
- No test framework is currently configured in this project

### Package Management
- `dotnet restore` - Restore NuGet packages
- `dotnet add package <PackageName>` - Add a new package

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