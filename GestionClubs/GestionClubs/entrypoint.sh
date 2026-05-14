#!/bin/bash
set -e

# Trust the self-signed certificate mounted at /https/aspnetapp.crt
if [ -f /https/aspnetapp.crt ]; then
    sudo cp /https/aspnetapp.crt /usr/local/share/ca-certificates/aspnetapp.crt
    sudo update-ca-certificates
fi

exec dotnet GestionClubs.API.dll
