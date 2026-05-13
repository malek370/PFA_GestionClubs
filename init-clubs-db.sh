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

# Run the seed script
echo "Running seed script..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStr0ngP@ssword!" -C -i /docker-entrypoint-initdb.d/seedGestionClub.sql
echo "Seed script completed."

# Keep SQL Server running in the foreground
wait
