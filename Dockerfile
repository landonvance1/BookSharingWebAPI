# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY BookSharingApp.csproj .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build BookSharingApp.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish BookSharingApp.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose port 8080
EXPOSE 8080

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "BookSharingApp.dll"]