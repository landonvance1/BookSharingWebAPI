#!/usr/bin/env pwsh
# Database setup script for BookSharing API

Write-Host "Setting up BookSharing database..." -ForegroundColor Green

# Check if psql is available
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "Error: psql not found. Please ensure PostgreSQL is installed and psql is in your PATH." -ForegroundColor Red
    exit 1
}

# Database configuration
$DB_NAME = "booksharingdb"
$DB_USER = "bookuser"

Write-Host "Creating database: $DB_NAME" -ForegroundColor Yellow

# Create SQL commands
$SQL_COMMANDS = @"
-- Force drop database if it exists (terminates active connections)
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid();
DROP DATABASE IF EXISTS $DB_NAME;

-- Create database
CREATE DATABASE $DB_NAME;

-- Grant permissions to application user
GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;

-- Connect to the new database and grant schema permissions
\c $DB_NAME
GRANT ALL ON SCHEMA public TO $DB_USER;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO $DB_USER;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO $DB_USER;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $DB_USER;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $DB_USER;
"@

# Execute SQL commands
try {
    Write-Host "Executing database setup commands..." -ForegroundColor Yellow
    $SQL_COMMANDS | psql -U postgres -h localhost -v ON_ERROR_STOP=1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database created successfully!" -ForegroundColor Green
        Write-Host "You can now run 'dotnet run' to apply migrations and seed data." -ForegroundColor Cyan
    } else {
        Write-Host "Database creation failed. Check PostgreSQL connection and permissions." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}