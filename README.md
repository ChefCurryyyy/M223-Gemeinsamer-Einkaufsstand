# 🛒 Co-Shop — Collaborative Shopping List

> Eine gemeinsame Einkaufslisten-App für Haushalte, WGs und Teams.  
> Mehrere Benutzer können gleichzeitig dieselbe Liste bearbeiten — Änderungen erscheinen in Echtzeit bei allen.

---

## Inhaltsverzeichnis

1. [Was ist Co-Shop?](#1-was-ist-co-shop)
2. [Voraussetzungen](#2-voraussetzungen)
3. [Schnellstart mit Docker](#3-schnellstart-mit-docker-empfohlen)
4. [Manuelle Installation (ohne Docker)](#4-manuelle-installation-ohne-docker)
5. [Wo läuft was?](#5-wo-läuft-was)
6. [Test-Accounts & Beispieldaten](#6-test-accounts--beispieldaten)

---

## 1. Was ist Co-Shop?

Co-Shop ist eine Web-App, mit der mehrere Personen gemeinsam Einkaufslisten verwalten können.

**Kernfunktionen:**
- 📝 Mehrere Einkaufslisten erstellen (z.B. „Wocheneinkauf", „Baumarkt", „Party")
- 👥 Andere Benutzer zu einer Liste einladen
- ✅ Artikel hinzufügen, bearbeiten und als „gekauft" abhaken
- ⚡ Echtzeit-Updates — wenn jemand anderes einen Artikel ändert, siehst du es sofort
- 🔒 Sicherer Login mit verschlüsseltem Passwort und JWT-Token

---

## 2. Voraussetzungen

### Für Docker-Start (empfohlen)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installiert und gestartet

Das ist alles. SQL Server, Backend und Frontend starten automatisch.

### Für manuelle Installation
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 20+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`
- MS SQL Server (lokal oder via Docker)
- [EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

---

## 3. Schnellstart mit Docker (empfohlen)

> ⏱️ Ca. 5 Minuten bis zur laufenden App.

### Schritt 1 — Repository klonen

```bash
git clone https://github.com/DEIN-USERNAME/M223-Gemeinsamer-Einkaufsstand.git
cd M223-Gemeinsamer-Einkaufsstand
```

### Schritt 2 — Alles starten

```bash
docker compose up --build
```

Alle Umgebungsvariablen haben funktionierende Standardwerte — kein `.env`-File nötig.  
Für Produktivbetrieb können `JWT_KEY` und `SA_PASSWORD` als Umgebungsvariablen gesetzt werden:

```env
SA_PASSWORD=CoShop_Dev_Password123!
JWT_KEY=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_MIN_32_CHARS
```

Beim **ersten Start** dauert es ca. 1–2 Minuten weil:
- Docker die Images baut
- SQL Server hochfährt
- Das Backend die Datenbank erstellt und Testdaten einspielt

Du siehst den Start wenn im Terminal steht:
```
coshop-backend | Database ready.
coshop-backend | Now listening on: http://[::]:8080
```

### Schritt 3 — App öffnen

🌐 **http://localhost** im Browser öffnen — fertig!

### Stoppen & neu starten

```bash
# Stoppen (Daten bleiben erhalten)
docker compose down

# Neu starten (ohne --build, da schon gebaut)
docker compose up

# Alles löschen inkl. Datenbank
docker compose down -v
```

---

## 4. Manuelle Installation (ohne Docker)

### Schritt 1 — SQL Server starten

Am einfachsten via Docker (nur SQL Server, kein ganzer Stack):

```bash
docker run \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD=Dev_Password123! \
  -p 1433:1433 \
  --name coshop-sql \
  -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

### Schritt 2 — Backend starten

```bash
cd CoShop

# Verbindungsstring prüfen (Passwort muss mit SQL Server übereinstimmen)
# Datei: appsettings.Development.json → ConnectionStrings:DefaultConnection

# Abhängigkeiten installieren
dotnet restore

# Datenbank erstellen und migrieren
dotnet ef database update

# Backend starten
dotnet run
```

✅ Backend läuft auf **http://localhost:5000**

### Schritt 3 — Frontend starten

```bash
cd coshop-frontend

# Abhängigkeiten installieren
npm install

# Dev-Server starten
ng serve
```

✅ Frontend läuft auf **http://localhost:4200**

> **Hinweis:** Bei manueller Installation zeigt das Frontend auf `http://localhost:5000/api`.  
> Falls der Backend-Port abweicht, `src/environments/environment.ts` anpassen.

---

## 5. Wo läuft was?

| Komponente | URL | Beschreibung |
|------------|-----|--------------|
| **Frontend** (Docker) | http://localhost | Angular App via Nginx |
| **Frontend** (manuell) | http://localhost:4200 | Angular Dev Server |
| **Backend API** | http://localhost:5000 | ASP.NET Core REST API |
| **Swagger UI** | http://localhost:5000/swagger | API-Dokumentation & Test-Interface |
| **SQL Server** | localhost:1433 | MS SQL Server Datenbank |

### Architektur-Überblick

```
Browser
  │
  ├─► http://localhost        (Nginx / Angular Frontend)
  │       │
  │       └─► /api/*          (Proxy → Backend)
  │       └─► /hubs/*         (Proxy → SignalR WebSocket)
  │
  └─► http://localhost:5000   (direkt, z.B. für Swagger)
          │
          └─► SQL Server :1433
```

---

## 6. Test-Accounts & Beispieldaten

Beim ersten Start werden automatisch 4 Benutzer und 3 Einkaufslisten mit Artikeln angelegt.

### Login-Daten

| Benutzername | E-Mail | Passwort | Rolle |
|---|---|---|---|
| **admin** | admin@coshop.dev | `Admin1234!` | Admin |
| **alice** | alice@coshop.dev | `Test1234!` | User |
| **bob** | bob@coshop.dev | `Test1234!` | User |
| **carol** | carol@coshop.dev | `Test1234!` | User |

### Vorgefertigte Listen

| Liste | Ersteller | Mitglieder | Artikel |
|---|---|---|---|
| Wocheneinkauf | alice | bob, carol | 10 Artikel (2 bereits gekauft) |
| Geburtstagsparty 🎉 | alice | — | 7 Artikel |
| Baumarkt | bob | alice | 5 Artikel (1 bereits gekauft) |

### Passwort-Anforderungen (für neue Accounts)

- Mindestens **8 Zeichen**
- Mindestens **1 Grossbuchstabe** (A–Z)
- Mindestens **1 Kleinbuchstabe** (a–z)
- Mindestens **1 Zahl** (0–9)
