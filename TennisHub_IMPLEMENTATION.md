# TennisHub – MVP Implementation Spec

## 1. Pregled projekta

Multi-tenant aplikacija za upravljanje tenis klubovima – web i mobilna platforma koje dijele kod. MVP pokriva:

- podršku za više klubova na jednoj platformi
- više razina rola (po klubu, ne globalno)
- evidenciju članova s odobravanjem pristupa od strane admina
- rezervacije terena s pravilima koja postavlja admin kluba

Liga modul (raspored natjecanja, unos rezultata, ljestvica) je **izvan opsega ovog MVP-a** i dodaje se naknadno, u zasebnoj fazi.

## 2. Tech stack

- **Baza / backend:** Supabase (PostgreSQL, Auth, Row Level Security, Realtime, Storage)
- **Web klijent:** Blazor WebAssembly (.NET 8/9)
- **Mobile klijent:** .NET MAUI Blazor Hybrid – dijeli Razor komponente s web klijentom, gradi se kao pravi APK/IPA bez zasebnog native koda
- **Komunikacija s bazom:** [supabase-csharp](https://github.com/supabase-community/supabase-csharp) klijent direktno iz Blazor/MAUI aplikacije. Nema zasebnog .NET API sloja za MVP – sigurnost i izolacija podataka po klubu rješava se isključivo preko RLS politika u Postgresu.
- **Autentikacija:** Supabase Auth (email/password za početak)

> Ako se kasnije pokaže potreba za poslovnom logikom koja ne pripada u bazu (slanje emaila, vanjske integracije), dodaje se tanki ASP.NET Core Web API sloj. Za MVP nije potreban.

## 3. Solution struktura

```
TennisHub.sln
├── src/
│   ├── TennisHub.Shared/        # Razor komponente, modeli, servisi – dijele Web i Mobile
│   ├── TennisHub.Web/           # Blazor WebAssembly host
│   ├── TennisHub.Mobile/        # .NET MAUI Blazor Hybrid host
│   └── TennisHub.Core/          # DTO-i, enumi, validacijska logika neovisna o UI-u
└── database/
    └── schema.sql               # Supabase shema (sekcija 6 ovog dokumenta)
```

## 4. Role i dozvole

| Rola | Opis |
|---|---|
| Super Admin | Rezervirano za buduću upotrebu (upravljanje platformom) – nije implementirano u MVP-u |
| Club Admin | Upravlja članovima, terenima i pravilima rezervacije unutar svog kluba. Može ih biti više po klubu. |
| Member | Korisnik odobren u klubu, rezervira terene po pravilima kluba. |

**Bitno:** rola je vezana uz **članstvo u klubu** (`club_memberships`), ne uz korisnika globalno. Isti korisnik može biti Member u jednom klubu, a Admin u drugom – korisnik može biti član više klubova istovremeno.

## 5. Domenski model

- **Profile** – proširenje Supabase Auth korisnika (ime, telefon)
- **Club** – klub (naziv, adresa)
- **ClubMembership** – spaja korisnika i klub: `role` (admin/member), `status` (pending/approved/rejected)
- **Court** – teren, pripada klubu (naziv, podloga, ima li reflektore)
- **ClubBookingRules** – po klubu: max sati po rezervaciji, max dana unaprijed (jedinstvena za sve terene u klubu)
- **Reservation** – rezervacija terena: teren, klub, korisnik, vrijeme početka/kraja, status (confirmed/cancelled)

## 6. Database schema (Supabase / PostgreSQL)

```sql
-- ============================================================
-- TennisHub MVP - Supabase/Postgres schema
-- ============================================================

create extension if not exists pgcrypto;

-- ------------------------------------------------------------
-- profiles (extends auth.users)
-- ------------------------------------------------------------
create table if not exists public.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  full_name text,
  phone text,
  created_at timestamptz not null default now()
);

-- ------------------------------------------------------------
-- clubs
-- ------------------------------------------------------------
create table if not exists public.clubs (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  address text,
  created_by uuid not null references auth.users(id),
  created_at timestamptz not null default now()
);

-- ------------------------------------------------------------
-- club_memberships
-- ------------------------------------------------------------
create table if not exists public.club_memberships (
  id uuid primary key default gen_random_uuid(),
  club_id uuid not null references public.clubs(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  role text not null default 'member' check (role in ('admin','member')),
  status text not null default 'pending' check (status in ('pending','approved','rejected')),
  joined_at timestamptz not null default now(),
  unique (club_id, user_id)
);

create index if not exists idx_club_memberships_user on public.club_memberships(user_id);
create index if not exists idx_club_memberships_club_status on public.club_memberships(club_id, status);

-- ------------------------------------------------------------
-- courts
-- ------------------------------------------------------------
create table if not exists public.courts (
  id uuid primary key default gen_random_uuid(),
  club_id uuid not null references public.clubs(id) on delete cascade,
  name text not null,
  surface_type text,
  has_floodlights boolean not null default false,
  created_at timestamptz not null default now()
);

-- ------------------------------------------------------------
-- club_booking_rules (1:1 s klubom)
-- ------------------------------------------------------------
create table if not exists public.club_booking_rules (
  club_id uuid primary key references public.clubs(id) on delete cascade,
  max_hours_per_booking numeric not null default 2,
  max_advance_days integer not null default 7
);

-- ------------------------------------------------------------
-- reservations
-- ------------------------------------------------------------
create table if not exists public.reservations (
  id uuid primary key default gen_random_uuid(),
  court_id uuid not null references public.courts(id) on delete cascade,
  club_id uuid not null references public.clubs(id) on delete cascade,
  user_id uuid not null references auth.users(id),
  start_time timestamptz not null,
  end_time timestamptz not null,
  status text not null default 'confirmed' check (status in ('confirmed','cancelled')),
  created_at timestamptz not null default now()
);

create index if not exists idx_reservations_court_time on public.reservations(court_id, start_time, end_time);

-- ============================================================
-- Helper functions
-- ============================================================

create or replace function public.is_club_admin(p_club_id uuid)
returns boolean
language sql
security definer
stable
set search_path = public
as $$
  select exists (
    select 1 from public.club_memberships
    where club_id = p_club_id and user_id = auth.uid()
      and role = 'admin' and status = 'approved'
  );
$$;

create or replace function public.is_club_member(p_club_id uuid)
returns boolean
language sql
security definer
stable
set search_path = public
as $$
  select exists (
    select 1 from public.club_memberships
    where club_id = p_club_id and user_id = auth.uid() and status = 'approved'
  );
$$;

-- ============================================================
-- Triggers
-- ============================================================

-- Auto-create profile on signup
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  insert into public.profiles (id, full_name)
  values (new.id, new.raw_user_meta_data->>'full_name');
  return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
after insert on auth.users
for each row execute function public.handle_new_user();

-- Auto-add club creator as approved admin + default booking rules
create or replace function public.handle_new_club()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  insert into public.club_memberships (club_id, user_id, role, status)
  values (new.id, new.created_by, 'admin', 'approved');

  insert into public.club_booking_rules (club_id)
  values (new.id);

  return new;
end;
$$;

drop trigger if exists on_club_created on public.clubs;
create trigger on_club_created
after insert on public.clubs
for each row execute function public.handle_new_club();

-- Validate reservation (booking rules + conflict check)
create or replace function public.validate_reservation()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_max_hours numeric;
  v_max_advance_days integer;
  v_conflict_count integer;
begin
  select club_id into new.club_id from public.courts where id = new.court_id;

  select max_hours_per_booking, max_advance_days
    into v_max_hours, v_max_advance_days
    from public.club_booking_rules
    where club_id = new.club_id;

  if v_max_hours is not null
     and (extract(epoch from (new.end_time - new.start_time)) / 3600.0) > v_max_hours then
    raise exception 'Rezervacija prelazi maksimalno dozvoljeno trajanje od % sati', v_max_hours;
  end if;

  if v_max_advance_days is not null
     and new.start_time > now() + (v_max_advance_days || ' days')::interval then
    raise exception 'Rezervacija je previse unaprijed (max % dana)', v_max_advance_days;
  end if;

  select count(*) into v_conflict_count
  from public.reservations
  where court_id = new.court_id
    and status = 'confirmed'
    and id <> coalesce(new.id, '00000000-0000-0000-0000-000000000000'::uuid)
    and tstzrange(start_time, end_time) && tstzrange(new.start_time, new.end_time);

  if v_conflict_count > 0 then
    raise exception 'Teren je vec rezerviran u tom terminu';
  end if;

  return new;
end;
$$;

drop trigger if exists before_reservation_change on public.reservations;
create trigger before_reservation_change
before insert or update on public.reservations
for each row execute function public.validate_reservation();

-- ============================================================
-- Row Level Security
-- ============================================================

alter table public.profiles enable row level security;
alter table public.clubs enable row level security;
alter table public.club_memberships enable row level security;
alter table public.courts enable row level security;
alter table public.club_booking_rules enable row level security;
alter table public.reservations enable row level security;

-- profiles
create policy "profiles_select" on public.profiles for select
using (
  id = auth.uid()
  or exists (
    select 1 from public.club_memberships cm1
    join public.club_memberships cm2 on cm1.club_id = cm2.club_id
    where cm1.user_id = auth.uid() and cm1.status = 'approved'
      and cm2.user_id = profiles.id and cm2.status = 'approved'
  )
);
create policy "profiles_insert_own" on public.profiles for insert with check (id = auth.uid());
create policy "profiles_update_own" on public.profiles for update using (id = auth.uid());

-- clubs
create policy "clubs_select_all" on public.clubs for select using (true);
create policy "clubs_insert_authenticated" on public.clubs for insert
with check (auth.uid() is not null and created_by = auth.uid());
create policy "clubs_update_admin" on public.clubs for update using (public.is_club_admin(id));

-- club_memberships
create policy "memberships_select" on public.club_memberships for select
using (user_id = auth.uid() or public.is_club_admin(club_id));
create policy "memberships_insert_self" on public.club_memberships for insert
with check (user_id = auth.uid() and role = 'member' and status = 'pending');
create policy "memberships_update_admin" on public.club_memberships for update
using (public.is_club_admin(club_id));
create policy "memberships_delete" on public.club_memberships for delete
using (public.is_club_admin(club_id) or user_id = auth.uid());

-- courts
create policy "courts_select_members" on public.courts for select
using (public.is_club_member(club_id) or public.is_club_admin(club_id));
create policy "courts_insert_admin" on public.courts for insert with check (public.is_club_admin(club_id));
create policy "courts_update_admin" on public.courts for update using (public.is_club_admin(club_id));
create policy "courts_delete_admin" on public.courts for delete using (public.is_club_admin(club_id));

-- club_booking_rules
create policy "rules_select_members" on public.club_booking_rules for select
using (public.is_club_member(club_id) or public.is_club_admin(club_id));
create policy "rules_update_admin" on public.club_booking_rules for update using (public.is_club_admin(club_id));

-- reservations
create policy "reservations_select" on public.reservations for select
using (user_id = auth.uid() or public.is_club_member(club_id) or public.is_club_admin(club_id));
create policy "reservations_insert_own" on public.reservations for insert
with check (user_id = auth.uid() and public.is_club_member(club_id));
create policy "reservations_update" on public.reservations for update
using (user_id = auth.uid() or public.is_club_admin(club_id));
```

## 7. Korisnički tokovi (MVP)

### 7.1 Registracija i prijava
Standardni Supabase Auth email/password signup + login. Nakon registracije korisnik vidi listu dostupnih klubova.

### 7.2 Kreiranje kluba
Bilo koji prijavljeni korisnik može kreirati novi klub. Kreator automatski postaje Club Admin (odobreno) – riješeno trigerom u bazi (`handle_new_club`), zajedno s defaultnim booking rules redom.

### 7.3 Pridruživanje klubu
Korisnik pregledava listu klubova i šalje zahtjev za pridruživanje (status = pending). Club Admin vidi listu zahtjeva na svom dashboardu, odobrava ili odbija. Korisnik vidi status svog zahtjeva.

### 7.4 Upravljanje terenima (Admin)
Admin dodaje/uređuje/briše terene svog kluba i postavlja pravila rezervacije za klub (max sati po rezervaciji, max dana unaprijed).

### 7.5 Rezervacija terena (Member)
Odobreni član bira teren, datum i vrijeme. Baza (trigger `validate_reservation`) provjerava: poklapanje s pravilima kluba (trajanje, koliko unaprijed) i sukob s postojećom rezervacijom. UI prikazuje grešku ako baza odbije insert. Član može otkazati vlastitu rezervaciju (status → cancelled).

## 8. Izvan opsega za MVP

- Liga modul (raspored natjecanja, unos rezultata, ljestvica)
- Plaćanja / članarine
- Notifikacije (email / push)
- Super Admin dashboard za platformu

## 9. Napomene za AI coding agenta

- Generiraj kod fazno (vidi popis ispod), testiraj svaku fazu prije prelaska na sljedeću
- UI tekst (labele, poruke korisniku) na hrvatskom jeziku; kod (klase, varijable, nazivi tablica) na engleskom
- Koristi nullable reference types i async/await konzistentno
- Supabase C# klijent: https://github.com/supabase-community/supabase-csharp

### Faze implementacije

1. Setup solution strukture (4 projekta), konfiguracija Supabase klijenta (env varijable za URL/anon key)
2. Auth flow (signup/login/logout) na Web i Mobile
3. Club CRUD + lista klubova + join request flow
4. Admin dashboard – pregled i odobravanje zahtjeva za članstvo
5. Court CRUD (admin)
6. Booking rules forma (admin)
7. Reservation flow (member) – kalendarski prikaz zauzetosti, kreiranje, otkazivanje
8. Osnovni styling i UX polish
