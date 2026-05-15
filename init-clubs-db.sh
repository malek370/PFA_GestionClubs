#!/bin/bash

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
for i in {1..30}; do
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -Q "SELECT 1" -C -b > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "SQL Server is ready."
        break
    fi
    echo "Not ready yet... retrying in 2s"
    sleep 2
done

# Create the GestionClubs database if it doesn't exist so the app can connect
echo "Ensuring GestionClubs database exists..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -C -Q \
    "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'GestionClubs') CREATE DATABASE GestionClubs;"
echo "Database ready."

# Wait for EF Core migrations to create the tables (gestionclubs app runs Migrate() on startup)
echo "Waiting for EF Core migrations to create tables..."
for i in {1..60}; do
    RESULT=$(/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -d GestionClubs -C \
        -Q "IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL PRINT 'ready'" 2>/dev/null | grep -c 'ready')
    if [ "$RESULT" -gt "0" ]; then
        echo "Tables found after $i attempts. Running seed script..."
        break
    fi
    echo "Tables not ready yet... retrying in 5s ($i/60)"
    sleep 5
done

# Run the seed script only if the Users table is empty (avoid duplicate data on restart)
echo "Checking if seed data is needed..."
COUNT=$(/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -d GestionClubs -C \
    -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Users;" 2>/dev/null | grep -oE '[0-9]+' | head -1)
if [ "$COUNT" = "0" ]; then
    echo "Running seed script..."
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -d GestionClubs -C \
        -i /docker-entrypoint-initdb.d/seedGestionClub.sql
    echo "Seed script completed."
else
    echo "Database already seeded ($COUNT users found). Skipping."
fi

# Keep SQL Server running in the foreground
wait
