# TMS2000 — TradeMasterSimulator2000

Jeu de simulation de trading : les joueurs achètent et revendent des ressources (carotte, blé, fer, or…) dont le prix évolue en continu, pendant qu'un administrateur pose des règles de marché (blocages, taxes, limites de quantité). Le jeu fonctionne aussi **hors ligne** : les actions faites sans réseau sont rejouées par le serveur à la reconnexion.

Projet fil rouge 4.0.3 — 3e année, UHA 4.0.

> **État actuel** : l'API est fonctionnelle, couverte par des tests et déployée automatiquement sur un serveur de l'UHA à chaque mise à jour de `main`. L'application mobile (.NET MAUI, Android) n'est pas encore développée.

---

## Sommaire

- [Fonctionnalités](#fonctionnalités)
- [Stack technique](#stack-technique)
- [Architecture](#architecture)
- [Démarrage rapide](#démarrage-rapide)
- [Configuration](#configuration)
- [Tests](#tests)
- [CI/CD](#cicd)
- [API](#api)
- [Choix techniques](#choix-techniques)
- [Feuille de route](#feuille-de-route)

---

## Fonctionnalités

**Joueur**
- Consulter le marché : prix actuels, variation, historique de prix (jusqu'à 7 jours), règles en vigueur.
- Acheter et vendre des ressources ; suivre son solde, son portefeuille et ses transactions.
- Jouer hors ligne : les actions sont envoyées en lot à la reconnexion et rejouées au **prix historique** du moment où elles ont été faites. Une vente gagnante faite hors ligne reçoit un bonus (+1 % du profit par heure hors ligne, plafonné à +20 %).
- Récupérer l'état complet du jeu (solde, portefeuille, transactions, prix récents, règles actives **et à venir**) pour le conserver sur le téléphone et jouer hors ligne avec des informations à jour.

**Administrateur**
- Créer des règles de marché sur une ou plusieurs ressources, immédiates ou programmées (durée de 1 à 168 h) :
  achat bloqué, vente bloquée, quantité maximale par transaction, taxe sur la transaction.
- Consulter la liste des joueurs et le détail de chacun.

**Moteur de prix**
- Un service d'arrière-plan fait évoluer les prix à intervalle régulier selon le « mode » de chaque ressource (stable, hausse lente ou rapide, baisse lente ou rapide, chaotique). Un prix ne descend jamais sous 1.

---

## Stack technique

| Domaine | Technologie |
|---|---|
| API | ASP.NET Core (.NET 8), contrôleurs REST |
| Base de données | MongoDB 7 (driver officiel MongoDB.Driver) |
| Authentification | JWT (access token) + refresh tokens avec rotation, mots de passe hashés en BCrypt |
| Documentation API | Swagger / OpenAPI (environnement Development) |
| Conteneurisation | Docker, Docker Compose (environnements dev, test, prod) |
| Tests | xUnit : tests d'intégration (WebApplicationFactory, vraie base MongoDB, aucun mock) et tests unitaires |
| CI/CD | GitLab CI (runner Docker) : build, formatage, tests, déploiement par SSH |
| Mobile (à venir) | .NET MAUI (Android), SQLite local |

---

## Architecture

```
TMS2000.sln
├── TMS2000.Shared    DTO et enums partagés entre l'API et le futur client mobile
├── TMS2000.Core      Logique métier : modèles, services, repositories, règles de calcul
├── TMS2000.Server    API ASP.NET Core : contrôleurs, gestion des erreurs, service de tick des prix
├── TMS2000.Console   Seeder : crée les ressources et les comptes de départ
├── TMS2000.Tests     Tests d'intégration et tests unitaires (xUnit)
└── TMS2000.Mobile    Application .NET MAUI (squelette, non commencée)
```

Une requête traverse les couches dans cet ordre :

```
Client HTTP → Controller (Server) → Service (Core) → Repository (Core) → MongoDB
```

- La logique de calcul pure (exécution d'un trade, évaluation des règles, simulation des prix) est isolée dans `TMS2000.Core/Helper`, sans dépendance à la base.
- Les erreurs métier sont des exceptions typées, traduites en réponses `ProblemDetails` (400, 401, 403, 404, 409, 422) par un gestionnaire global.

---

## Démarrage rapide

### Prérequis

- [Docker](https://docs.docker.com/get-docker/) avec le plugin Compose
- [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) (pour lancer les tests ou développer hors Docker)

### 1. Configurer l'environnement de développement

```bash
cp .env.dev.example .env.dev
```

Remplir les valeurs vides de `.env.dev` :

```bash
openssl rand -hex 24      # pour MONGO_PASSWORD (hexadécimal uniquement : il est placé dans l'URI Mongo)
openssl rand -base64 32   # pour JWT_SECRET
```

ainsi que `SEED_ADMIN_PASSWORD` et `SEED_PLAYER_PASSWORD`, au choix.

> Les fichiers `.env.dev`, `.env.test` et `.env.prod` ne sont jamais commités : seuls les `.env.*.example` le sont.

### 2. Lancer l'API et MongoDB

```bash
docker compose --env-file .env.dev \
  -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

### 3. Remplir la base

```bash
docker compose --env-file .env.dev \
  -f docker-compose.yml -f docker-compose.dev.yml \
  --profile seed run --rm --build tms2000.console
```

Le seeder crée 10 ressources, un compte administrateur et un compte joueur (solde de départ : 1 000), avec les identifiants définis dans `.env.dev`. Il peut être relancé sans risque : ce qui existe déjà est ignoré.

### 4. Utiliser l'API

- Swagger : <http://localhost:5000/swagger> (le port est `API_PORT` dans `.env.dev`)
- Santé de l'API et de la base : <http://localhost:5000/health>

Dans Swagger : appeler `POST /api/auth/login`, copier l'`accessToken` de la réponse, puis le coller dans le bouton **Authorize**.

---

## Configuration

Chaque environnement a son fichier : `.env.dev`, `.env.test`, `.env.prod`, construit à partir du `.example` correspondant.

| Variable | Rôle |
|---|---|
| `API_PORT` | Port de l'API sur la machine hôte (le conteneur écoute sur 8080) |
| `MONGO_PORT` | Port de MongoDB sur la machine hôte (dev et test uniquement ; non exposé en prod) |
| `MONGO_USERNAME` / `MONGO_PASSWORD` | Compte administrateur MongoDB |
| `MONGO_DATABASE` | Nom de la base |
| `JWT_SECRET` | Clé de signature des JWT (32 octets minimum) |
| `JWT_EXPIRY_HOURS` | Durée de vie d'un access token |
| `JWT_REFRESH_TOKEN_DAYS` | Durée de vie d'un refresh token |
| `PRICE_TICK_SECONDS` | Intervalle entre deux évolutions de prix |
| `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | Compte administrateur créé par le seeder |
| `SEED_PLAYER_EMAIL` / `SEED_PLAYER_PASSWORD` | Compte joueur créé par le seeder |

Docker Compose transmet ces valeurs à l'API sous forme de variables d'environnement (`ConnectionStrings__MongoDb`, `Jwt__Secret`…). L'API refuse de démarrer si l'une d'elles est manquante ou vide.

---

## Tests

72 tests, répartis en deux niveaux :

- **64 tests d'intégration** couvrent toutes les routes de l'API : authentification et sessions, marché, trades, règles de marché (conditions et effets), synchronisation dans les deux sens, administration, ainsi que le moteur de prix (enregistrement des ticks en base et déclenchement par le timer). Ils démarrent la **vraie API en mémoire** (`WebApplicationFactory`) et l'interrogent par HTTP, contre une **vraie base MongoDB** : aucun mock.
- **8 tests unitaires** vérifient le calcul des prix (`PriceSimulator`) : bornes de variation de chaque mode, tendance moyenne, prix plancher, changements de mode. Le générateur aléatoire est initialisé avec une graine fixe, pour des résultats reproductibles.

### Lancer les tests en local

```bash
cp .env.test.example .env.test

# MongoDB de test, sur le port 27018 (ne gêne pas celui de dev)
docker compose -p tms2000-test --env-file .env.test \
  -f docker-compose.yml -f docker-compose.test.yml up -d tms2000.mongodb

dotnet test TMS2000.Tests --logger "console;verbosity=normal"
```

### Fonctionnement

- Une seule API et une seule base sont préparées pour toute la suite (collection fixture xUnit) ; la base est vidée et remplie de données connues au démarrage.
- Chaque test crée son propre joueur, et si besoin sa propre ressource : les tests ne dépendent pas les uns des autres.
- Les tests du moteur de prix utilisent une base séparée : un tick modifie toutes les ressources, ce qui fausserait les autres tests.
- La connexion à MongoDB est lue dans la variable `TMS2000_TEST_MONGO` (par défaut `mongodb://localhost:27018`), ce qui permet d'utiliser la même suite en local et en CI.
- Les tests unitaires (`Tests/Unit`) n'ont besoin d'aucune base de données.

---

## CI/CD

La pipeline GitLab (`.gitlab-ci.yml`) s'exécute à chaque push, sur un runner Docker :

| Stage | Job | Rôle |
|---|---|---|
| `build` | `build` | Compile l'API, le cœur métier, les tests et le seeder |
| `quality` | `Quality` | Vérifie le formatage du code (`dotnet format --verify-no-changes`) |
| `test` | `integration-tests` | Lance tous les tests (intégration et unitaires) contre un service MongoDB 7 ; le rapport JUnit est affiché dans l'onglet « Tests » de la pipeline |
| `deploy` | `Deploy` | Branche `main` uniquement : déploie l'API et MongoDB sur le serveur de production |

Le projet MAUI est exclu de la pipeline : l'image .NET utilisée ne contient pas le workload MAUI.

### Déploiement continu

Chaque mise à jour de `main` est déployée automatiquement, une fois le build, le formatage et les tests validés :

1. le job se connecte en SSH au serveur de production avec une clé dédiée, stockée dans une variable GitLab protégée (disponible uniquement pour `main`), et vérifie l'empreinte du serveur ;
2. il envoie les fichiers suivis par git (`git archive`) dans le dossier du projet sur le serveur ;
3. il lance `docker compose up -d --build --wait` : le serveur reconstruit l'image de l'API et redémarre les conteneurs, puis le job attend que leurs healthchecks (dont `/health`) soient au vert. Si l'API ne démarre pas, le déploiement échoue.

Les secrets de production (`.env.prod`) restent uniquement sur le serveur : ils ne sont ni dans le dépôt, ni dans GitLab. En production, MongoDB n'est pas exposé hors du réseau Docker, et Swagger est désactivé.

---

## API

| Méthode | Route | Accès | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Connexion : renvoie un access token et un refresh token |
| `POST` | `/api/auth/refresh` | Public | Échange un refresh token contre une nouvelle paire de tokens |
| `POST` | `/api/auth/logout` | Public | Révoque un refresh token |
| `GET` | `/api/market` | Connecté | Liste des ressources avec prix, variation et règles actives |
| `GET` | `/api/market/{resourceId}` | Connecté | Détail d'une ressource |
| `GET` | `/api/market/{resourceId}/history?hours=24` | Connecté | Historique de prix (1 à 168 h) |
| `GET` | `/api/me` | Joueur | Solde, portefeuille et transactions récentes |
| `POST` | `/api/trades` | Joueur | Achat ou vente immédiat |
| `GET` | `/api/sync` | Joueur | État complet du joueur et du marché, à conserver sur le téléphone |
| `POST` | `/api/sync` | Joueur | Envoi d'un lot d'actions faites hors ligne ; renvoie aussi l'état à jour |
| `GET` | `/api/admin/rules` | Admin | Règles actives et à venir |
| `POST` | `/api/admin/rules` | Admin | Création d'une règle |
| `DELETE` | `/api/admin/rules/{ruleId}` | Admin | Suppression d'une règle |
| `GET` | `/api/admin/users` | Admin | Liste des joueurs |
| `GET` | `/api/admin/users/{userId}` | Admin | Détail d'un joueur |
| `GET` | `/health` | Public | État de l'API et de MongoDB |

Le détail des corps de requête et de réponse est disponible dans Swagger.

---

## Choix techniques

- **Concurrence optimiste** : chaque compte joueur porte un numéro de version. Une écriture n'est appliquée que si la version n'a pas changé depuis la lecture ; sinon l'opération est relancée (3 tentatives maximum).
- **Idempotence** : chaque action porte un identifiant généré par le client. Une action déjà traitée renvoie le même résultat sans être exécutée une seconde fois, ce qui rend les nouvelles tentatives sans risque après une coupure réseau.
- **Synchronisation dans les deux sens** : le téléphone envoie des **intentions** (les actions faites hors ligne) et reçoit l'**état** calculé par le serveur, qui reste la seule source de vérité. Il n'y a donc jamais de conflit de données à résoudre : une action invalide est simplement refusée.
  - *Envoi (push)* : les actions sont rejouées dans l'ordre chronologique, au prix et avec les règles en vigueur au moment où le joueur les a faites. Une action invalide est refusée individuellement, avec sa raison, sans faire échouer le lot. Le lot entier est écrit en une seule opération.
  - *Réception (pull)* : l'état envoyé au téléphone inclut les règles à venir, pour que le joueur hors ligne soit prévenu d'une règle qui démarrera avant sa prochaine connexion. Seuls les derniers points de prix sont envoyés, pour limiter le volume de données.
  - Une règle créée pendant que le joueur est hors ligne ne peut pas être connue de son téléphone : ses actions concernées seront refusées à la synchronisation. C'est le comportement attendu.
- **Refresh tokens** : seul leur hash SHA-256 est stocké ; chaque utilisation en délivre un nouveau (rotation) ; 5 sessions maximum par compte.
- **Lots FIFO** : les achats sont conservés par lots, et les ventes consomment les lots les plus anciens en premier, ce qui permet de calculer le profit réel de chaque vente.
- **Efficacité d'une vente** : profit réalisé divisé par le meilleur profit qui était possible pendant la détention des lots vendus. Pour chaque lot, ce meilleur profit vaut au minimum zéro (revendre aussitôt) : un lot dont le prix n'a fait que baisser ne réduit pas le dénominateur.

---

## Feuille de route

- [x] API : authentification, marché, trades, règles, synchronisation hors ligne
- [x] Tests d'intégration et tests unitaires
- [x] CI : build, qualité, tests
- [x] Déploiement continu sur le serveur de l'UHA
- [x] Synchronisation serveur → mobile (état complet du joueur et règles à venir)
- [ ] Application mobile .NET MAUI avec base SQLite locale et mode hors ligne

---

## Auteur

Bertrand Beaujard — UHA 4.0
