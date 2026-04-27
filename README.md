# PFA_GestionClubs
Plateforme de gestion des associations / clubs universitaires avec Chatbot

## Lancer l'application
Pour lancer le backend et ses services via Docker Compose, exécutez la commande suivante à la racine du projet :
créer le cerif https : 
```bash
mkdir -p ./certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout ./certs/aspnetapp.key \
  -out ./certs/aspnetapp.crt \
  -subj "//CN=localhost"

# Fixer les permissions pour l'utilisateur non- root de Docker
chmod 644 ./certs/aspnetapp.key ./certs/aspnetapp.crt ./certs/private_key.pem ./certs/public_key.pem
```
lancer les contenaires
```bash
docker compose up -d --pull always
```

# Ce repo est designé pour le backend
## main branch changeable only with pull request