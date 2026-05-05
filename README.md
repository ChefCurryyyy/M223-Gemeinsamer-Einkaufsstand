# M223-Gemeinsamer-Einkaufsstand
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
7. [Die App benutzen](#7-die-app-benutzen)
8. [Echtzeit-Synchronisation testen](#8-echtzeit-synchronisation-testen)
9. [API & Swagger](#9-api--swagger)
10. [Häufige Probleme](#10-häufige-probleme)
11. [Tech Stack](#11-tech-stack)

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
git clone https://github.com/DEIN-USERNAME/M223.git
cd M223
```

### Schritt 2 — Umgebungsvariablen konfigurieren

```bash
# Beispiel-Datei kopieren
cp .env.example .env
```

Die `.env` Datei kann so belassen werden — die Standardwerte funktionieren direkt.  
Für Produktivbetrieb sollte `JWT_KEY` und `SA_PASSWORD` geändert werden.

```env
SA_PASSWORD=CoShop_Dev_Password123!
JWT_KEY=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_MIN_32_CHARS
```

### Schritt 3 — Alles starten

```bash
docker-compose up --build
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

### Schritt 4 — App öffnen

🌐 **http://localhost** im Browser öffnen — fertig!

### Stoppen & neu starten

```bash
# Stoppen (Daten bleiben erhalten)
docker-compose down

# Neu starten (ohne --build, da schon gebaut)
docker-compose up

# Alles löschen inkl. Datenbank
docker-compose down -v
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

# Datenbank erstellen (beim ersten Mal)
dotnet ef migrations add InitialCreate   # falls noch keine Migration vorhanden
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

---

## 7. Die App benutzen

### Registrieren & Anmelden

1. **http://localhost** aufrufen
2. Auf „Registrieren" klicken → neuen Account erstellen  
   — oder direkt einen Test-Account verwenden (z.B. `alice@coshop.dev` / `Test1234!`)
3. Nach dem Login landet man auf dem **Dashboard** mit allen eigenen und geteilten Listen

### Listen verwalten

| Aktion | Wie |
|--------|-----|
| Neue Liste erstellen | Dashboard → „Neue Liste" Button |
| Liste öffnen | Auf eine Listenkarte klicken |
| Liste umbenennen | In der Liste → `⋮` Menü → „Umbenennen" |
| Liste löschen | In der Liste → `⋮` Menü → „Liste löschen" |

### Artikel verwalten

| Aktion | Wie |
|--------|-----|
| Artikel hinzufügen | In der Liste → „Artikel hinzufügen" Button |
| Artikel bearbeiten | Auf `⋮` neben dem Artikel → „Bearbeiten" |
| Artikel als gekauft markieren | Checkbox neben dem Artikel anklicken |
| Artikel löschen | Auf `⋮` neben dem Artikel → „Löschen" |

### Mitglieder einladen

1. Eine Liste öffnen (nur Ersteller kann einladen)
2. Im Bereich „Mitglieder" → „Einladen" Button
3. **Benutzernamen** (nicht E-Mail) eingeben und bestätigen
4. Die eingeladene Person sieht die Liste ab sofort in ihrem Dashboard

> **Beispiel:** Als `alice` eingeloggt → Liste „Wocheneinkauf" öffnen → `bob` oder `carol` einladen

---

## 8. Echtzeit-Synchronisation testen

Co-Shop zeigt Änderungen sofort allen aktiven Benutzern — ohne Seite neu zu laden.

### So testen:

1. **Zwei Browser-Fenster** öffnen (oder Browser + Inkognito-Tab)
2. Im ersten Fenster als **alice** (`alice@coshop.dev`) anmelden
3. Im zweiten Fenster als **bob** (`bob@coshop.dev`) anmelden
4. Beide navigieren zur Liste **„Wocheneinkauf"**
5. Alice hakt einen Artikel ab → Bob sieht es sofort mit einem blauen Banner:  
   _„alice hat 'Milch' gekauft ✓"_

### Was wird in Echtzeit synchronisiert?

| Ereignis | Was passiert beim anderen Benutzer |
|---|---|
| Artikel hinzugefügt | Erscheint sofort in der Liste |
| Artikel bearbeitet | Aktualisiert sich direkt |
| Artikel als gekauft markiert | Wandert in die „Erledigt"-Sektion |
| Artikel gelöscht | Verschwindet sofort |
| Liste umbenannt | Titel ändert sich im Header |
| Liste gelöscht | Benutzer wird zum Dashboard weitergeleitet |
| Mitglied hinzugefügt/entfernt | Mitgliederliste aktualisiert sich |

### Konfliktschutz

Wenn zwei Benutzer **gleichzeitig** denselben Artikel abhaken wollen, zeigt die App ein **Schloss-Symbol** 🔒 beim betroffenen Artikel. Der zweite Klick wird abgefangen und eine Fehlermeldung erscheint — so werden inkonsistente Daten verhindert.

---

## 9. API & Swagger

Die komplette API kann direkt im Browser getestet werden.

### Swagger UI öffnen

👉 **http://localhost:5000/swagger**

### Mit JWT in Swagger authentifizieren

1. Erst `POST /api/auth/login` aufrufen mit:
   ```json
   {
     "email": "alice@coshop.dev",
     "password": "Test1234!"
   }
   ```
2. Den zurückgegebenen `token` kopieren
3. Oben rechts auf **„Authorize"** klicken
4. `Bearer DEIN_TOKEN` eingeben und bestätigen
5. Alle geschützten Endpoints sind nun zugänglich

### Alle Endpoints im Überblick

| Methode | Endpoint | Auth | Beschreibung |
|---------|----------|------|--------------|
| `POST` | `/api/auth/register` | — | Neuen Account erstellen |
| `POST` | `/api/auth/login` | — | Anmelden, JWT erhalten |
| `GET` | `/api/shoppinglists` | JWT | Alle eigenen + geteilten Listen |
| `POST` | `/api/shoppinglists` | JWT | Neue Liste erstellen |
| `GET` | `/api/shoppinglists/{id}` | JWT | Listendetails mit Artikeln |
| `PUT` | `/api/shoppinglists/{id}` | JWT | Liste umbenennen |
| `DELETE` | `/api/shoppinglists/{id}` | JWT | Liste löschen |
| `POST` | `/api/shoppinglists/{id}/members` | JWT | Mitglied einladen |
| `DELETE` | `/api/shoppinglists/{id}/members/{userId}` | JWT | Mitglied entfernen |
| `POST` | `/api/shoppinglists/{id}/items` | JWT | Artikel hinzufügen |
| `GET` | `/api/shoppinglists/{id}/items/{itemId}` | JWT | Einzelnen Artikel abrufen |
| `PUT` | `/api/shoppinglists/{id}/items/{itemId}` | JWT | Artikel bearbeiten |
| `PATCH` | `/api/shoppinglists/{id}/items/{itemId}/bought` | JWT | Gekauft-Status umschalten |
| `DELETE` | `/api/shoppinglists/{id}/items/{itemId}` | JWT | Artikel löschen |
| `GET` | `/api/admin/users` | Admin | Alle Benutzer (nur Admin-Rolle) |

---

## 10. Häufige Probleme

### App lädt nicht / weisse Seite

→ Prüfen ob alle Container laufen:
```bash
docker-compose ps
```
Alle drei sollten `running` zeigen: `coshop-sqlserver`, `coshop-backend`, `coshop-frontend`

### Backend startet nicht (SQL Server nicht erreichbar)

→ SQL Server braucht beim ersten Start ca. 30–45 Sekunden.  
Das Backend versucht es automatisch 10× mit Wartezeit. Einfach abwarten.  
Im Terminal sollte nach ca. 1 Minute stehen: `Database ready.`

### Login schlägt fehl

→ Prüfen ob die Testdaten korrekt eingegeben wurden (Gross-/Kleinschreibung beachten):
- E-Mail: `alice@coshop.dev` (alles klein)
- Passwort: `Test1234!` (T gross, ! am Ende)

### Echtzeit funktioniert nicht

→ Prüfen ob der Browser WebSockets blockiert (z.B. Unternehmens-Proxy).  
SignalR fällt automatisch auf Long-Polling zurück, Echtzeit-Updates funktionieren aber evtl. mit etwas Verzögerung.

### Port 80 bereits belegt (Windows/Mac)

→ In `docker-compose.yml` den Frontend-Port ändern:
```yaml
frontend:
  ports:
    - "8080:80"   # statt "80:80"
```
Dann unter **http://localhost:8080** aufrufen.

### Datenbank zurücksetzen

```bash
# Alle Daten löschen und neu starten (Testdaten werden neu eingespielt)
docker-compose down -v
docker-compose up --build
```

---

## 11. Tech Stack

| Schicht | Technologie | Version |
|---------|-------------|---------|
| Frontend | Angular | 17 |
| UI-Komponenten | Angular Material | 17 |
| Backend | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Datenbank | Microsoft SQL Server | 2022 |
| Echtzeit | SignalR (WebSockets) | — |
| Authentifizierung | JWT Bearer Token | — |
| Passwort-Hashing | BCrypt | — |
| API-Dokumentation | Swagger / Swashbuckle | 6.5 |
| Container | Docker + docker-compose | — |
| Reverse Proxy | Nginx (Alpine) | — |
