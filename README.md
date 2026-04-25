# PFA_GestionClubs
Plateforme de gestion des associations / clubs universitaires avec Chatbot

## Lancer l'application
Pour lancer le backend et ses services via Docker Compose, exécutez les commande suivante à la racine du projet :
creer le certif https avec 
```bash
mkdir -p ./certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout ./certs/aspnetapp.key \
  -out ./certs/aspnetapp.crt \
  -subj "//CN=localhost"
```
Lancer les contenaire
```bash
docker compose up -d --pull always
```

# Ce repo est designé pour le backend
## main branch changeable only with pull request