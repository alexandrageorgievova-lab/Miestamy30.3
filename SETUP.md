# MiestaMy – Setup na novom počítači

> Tento súbor je tvoj kompletný návod ako rozbehať projekt od nuly na novom Macu.

---

## 1. Čo nainštalovať

### .NET 9.0 SDK
https://dotnet.microsoft.com/en-us/download/dotnet/9.0

Stiahni **macOS Arm64** (Apple Silicon / M-chip) alebo **macOS x64** (Intel).
Po inštalácii over v Terminali:
```
dotnet --version
```
Musí písať `9.x.x`.

---

### JetBrains Rider (IDE)
https://www.jetbrains.com/rider/download/

Stiahni, nainštaluj, aktivuj licenciou / free trial / JetBrains Education account.

---

### Git
Na Macu je git zvyčajne predinštalovaný. Over:
```
git --version
```
Ak nie: https://git-scm.com/download/mac

---

### Nakonfiguruj git (ak ešte nie je):
```
git config --global user.name "alexandrageorgievova"
git config --global user.email "tvoj@email.com"
```

---

## 2. Stiahni projekt z GitHubu

```bash
cd ~/Documents
git clone https://github.com/alexandrageorgievova-lab/Miestamy30.3.git
cd Miestamy30.3
```

---

## 3. Vytvor chýbajúce lokálne konfig súbory

Tieto súbory sú v `.gitignore` (necommitujú sa), musíš ich vytvoriť raz:

### `Miestamy30.3/Miestamy30.3/Properties/launchSettings.json`
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5176",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7187;http://localhost:5176",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### `Miestamy30.3/Miestamy30.3/appsettings.Development.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 4. Spusti projekt

V Terminali (alebo priamo cez Rider — tlačidlo ▶):
```bash
cd Miestamy30.3/Miestamy30.3
dotnet run
```

App pobeží na: **http://localhost:5176**

SQLite databáza (`miestamy.db`) sa vytvorí automaticky pri prvom spustení — nemusíš robiť nič.

---

## 5. Railway (produkcia / deployment)

Produkčná verzia beží na Railway a **nepotrebuje nič extra na tvojom počítači** — Railway sa napojí na GitHub sám.

- **Dashboard:** https://railway.app/project/ddf67c5f-2c13-4ee4-8803-f719e54d2c4e
- **Deploy:** automaticky po každom `git push origin main`
- **Databáza:** PostgreSQL je na Railway, dáta sú tam uložené

Ak by si potrebovala znova nastaviť Railway CLI:
```bash
brew install railway
railway login
```

---

## 6. Workflow po novom nastavení

```bash
# Pred prácou – stiahni posledné zmeny
git pull origin main

# Po práci – commitni a pushni
git add .
git commit -m "popis zmeny"
git push origin main
```

Po `git push` sa Railway automaticky predeployuje (cca 2–3 min).

---

## 7. Štruktúra projektu (rýchla orientácia)

```
Miestamy30.3/
├── Miestamy30.3/
│   ├── Controllers/       ← API endpointy
│   ├── Data/              ← DbConnectionFactory, DatabaseInitializer (seed)
│   ├── Models/            ← C# triedy (Miesto, Kategoria, Filter...)
│   ├── Repositories/      ← databázové operácie (Dapper)
│   ├── wwwroot/
│   │   ├── index.html     ← celý frontend (Leaflet mapa, sidebar, detail panel)
│   │   └── fonts/         ← MMFont.woff2 (custom font)
│   ├── Program.cs         ← registrácia služieb, middleware
│   └── appsettings.json   ← connection string (SQLite lokálne)
├── Dockerfile             ← Railway build
└── SETUP.md               ← tento súbor
```

---

## 8. Technológie v projekte

| Čo | Čím |
|----|-----|
| Backend | ASP.NET Core 9.0 (C#) |
| Databáza lokálne | SQLite cez `Microsoft.Data.Sqlite` |
| Databáza produkcia | PostgreSQL cez `Npgsql` |
| ORM | Dapper |
| Frontend | Vanilla JS + Leaflet.js |
| Font | MMFont (vlastný WOFF2) |
| Hosting | Railway |
| Repo | GitHub |
