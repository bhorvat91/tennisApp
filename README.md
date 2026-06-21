# TennisHub MVP

Ovaj repository pokriva TennisHub MVP faze 1–5 (setup, auth, klubovi, admin odobravanje članstva, court CRUD) kroz projekte:

- `src/TennisHub.Core`
- `src/TennisHub.Shared`
- `src/TennisHub.Web`

## Supabase konfiguracija

1. Uredi lokalnu datoteku `src/TennisHub.Web/wwwroot/appsettings.Development.json`:

```json
{
  "Supabase": {
    "Url": "https://YOUR-PROJECT.supabase.co",
    "AnonKey": "YOUR_ANON_KEY"
  }
}
```

2. U commitanom `appsettings.json` vrijednosti ostaju prazne (`""`).

## Baza podataka (Supabase)

1. Otvori Supabase SQL editor.
2. Kopiraj sadržaj iz `database/schema.sql`.
3. Pokreni SQL skriptu bez izmjena.

## Pokretanje Web aplikacije

```bash
cd src/TennisHub.Web
dotnet run
```

Aplikacija će biti dostupna na URL-u koji ispiše `dotnet run`.
