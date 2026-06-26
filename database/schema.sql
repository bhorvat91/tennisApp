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
  default_club_id uuid references public.clubs(id) on delete set null,
  avatar_url text,
  racket text,
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
  membership_type text not null default 'member' check (membership_type in ('member','guest')),
  fee_paid boolean not null default false,
  can_reserve boolean not null default true,
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
  min_hours_per_booking numeric not null default 0.5,
  max_hours_per_booking numeric not null default 2,
  max_advance_days integer not null default 7
);

alter table if exists public.club_booking_rules
  add column if not exists min_hours_per_booking numeric not null default 0.5,
  add column if not exists max_hours_per_booking numeric not null default 2,
  add column if not exists max_advance_days integer not null default 7;

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
  v_min_hours numeric;
  v_max_hours numeric;
  v_max_advance_days integer;
  v_conflict_count integer;
begin
  select club_id into new.club_id from public.courts where id = new.court_id;

  select min_hours_per_booking, max_hours_per_booking, max_advance_days
    into v_min_hours, v_max_hours, v_max_advance_days
    from public.club_booking_rules
    where club_id = new.club_id;

  if v_min_hours is not null
     and (extract(epoch from (new.end_time - new.start_time)) / 3600.0) < v_min_hours then
    raise exception 'Rezervacija je kraca od minimalno dozvoljenog trajanja od % sati', v_min_hours;
  end if;

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
