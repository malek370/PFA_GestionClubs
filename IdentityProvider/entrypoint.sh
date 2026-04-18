#!/bin/sh

# Générer les clés RSA si elles n'existent pas
if [ ! -f /app/keys/private_key.pem ]; then
  echo "Generating RSA keys..."
  openssl genrsa -out /app/keys/private_key.pem 2048
  openssl rsa -in /app/keys/private_key.pem -pubout -outform PEM | \
    openssl rsa -pubin -RSAPublicKey_out -out /app/keys/public_key.pem
fi

# Créer des liens symboliques dans /app
ln -sf /app/keys/private_key.pem /app/private_key.pem
ln -sf /app/keys/public_key.pem /app/public_key.pem

exec dotnet IdentityProvider.dll