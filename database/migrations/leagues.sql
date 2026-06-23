-- ============================================================
-- Liga sustav — inkrementalna migracija
-- Pokreni jednom u Supabase SQL editoru
-- ============================================================

-- ------------------------------------------------------------
-- leagues
-- ------------------------------------------------------------
create table if not exists public.leagues (
  id uuid primary key default gen_random_uuid(),
  club_id uuid not null references public.clubs(id) on delete cascade,
  name text not null,
  description text,
  season text,
  status text not null default 'active' check (status in ('active', 'finished')),
  created_by uuid not null references auth.users(id),
  created_at timestamptz not null default now()
);

create index if not exists idx_leagues_club on public.leagues(club_id);

-- ------------------------------------------------------------
-- league_players
-- ------------------------------------------------------------
create table if not exists public.league_players (
  id uuid primary key default gen_random_uuid(),
  league_id uuid not null references public.leagues(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  status text not null default 'active' check (status in ('active', 'withdrawn')),
  joined_at timestamptz not null default now(),
  unique (league_id, user_id)
);

create index if not exists idx_league_players_league on public.league_players(league_id);

-- ------------------------------------------------------------
-- league_matches
-- ------------------------------------------------------------
create table if not exists public.league_matches (
  id uuid primary key default gen_random_uuid(),
  league_id uuid not null references public.leagues(id) on delete cascade,
  player1_id uuid not null references auth.users(id),
  player2_id uuid not null references auth.users(id),
  scheduled_date date,
  status text not null default 'pending' check (status in ('pending', 'completed', 'cancelled')),
  created_at timestamptz not null default now(),
  check (player1_id <> player2_id)
);

create index if not exists idx_league_matches_league on public.league_matches(league_id);

-- ------------------------------------------------------------
-- league_match_results
-- ------------------------------------------------------------
create table if not exists public.league_match_results (
  id uuid primary key default gen_random_uuid(),
  match_id uuid not null references public.league_matches(id) on delete cascade unique,
  winner_id uuid not null references auth.users(id),
  score text,
  entered_by uuid not null references auth.users(id),
  entered_at timestamptz not null default now()
);

-- ============================================================
-- RLS
-- ============================================================

alter table public.leagues enable row level security;
alter table public.league_players enable row level security;
alter table public.league_matches enable row level security;
alter table public.league_match_results enable row level security;

-- leagues: svi odobreni članovi kluba čitaju, samo admin kreira/briše
create policy "leagues_select_members" on public.leagues for select
  using (public.is_club_member(club_id));

create policy "leagues_insert_admin" on public.leagues for insert
  with check (public.is_club_admin(club_id));

create policy "leagues_update_admin" on public.leagues for update
  using (public.is_club_admin(club_id));

create policy "leagues_delete_admin" on public.leagues for delete
  using (public.is_club_admin(club_id));

-- league_players: svi odobreni članovi kluba čitaju, sami se prijavljuju/povlače
create policy "league_players_select" on public.league_players for select
  using (exists (
    select 1 from public.leagues l
    where l.id = league_id and public.is_club_member(l.club_id)
  ));

create policy "league_players_insert_self" on public.league_players for insert
  with check (
    user_id = auth.uid()
    and exists (
      select 1 from public.leagues l
      where l.id = league_id and public.is_club_member(l.club_id)
    )
  );

create policy "league_players_update_self_or_admin" on public.league_players for update
  using (
    user_id = auth.uid()
    or exists (
      select 1 from public.leagues l
      where l.id = league_id and public.is_club_admin(l.club_id)
    )
  );

-- league_matches: svi odobreni članovi kluba čitaju, admin upravlja
create policy "league_matches_select" on public.league_matches for select
  using (exists (
    select 1 from public.leagues l
    where l.id = league_id and public.is_club_member(l.club_id)
  ));

create policy "league_matches_insert_admin" on public.league_matches for insert
  with check (exists (
    select 1 from public.leagues l
    where l.id = league_id and public.is_club_admin(l.club_id)
  ));

create policy "league_matches_update_admin" on public.league_matches for update
  using (exists (
    select 1 from public.leagues l
    where l.id = league_id and public.is_club_admin(l.club_id)
  ));

create policy "league_matches_delete_admin" on public.league_matches for delete
  using (exists (
    select 1 from public.leagues l
    where l.id = league_id and public.is_club_admin(l.club_id)
  ));

-- league_match_results: svi odobreni članovi čitaju, igrači upisuju rezultate svojih mečeva
create policy "match_results_select" on public.league_match_results for select
  using (exists (
    select 1 from public.league_matches lm
    join public.leagues l on l.id = lm.league_id
    where lm.id = match_id and public.is_club_member(l.club_id)
  ));

create policy "match_results_insert_player" on public.league_match_results for insert
  with check (
    entered_by = auth.uid()
    and exists (
      select 1 from public.league_matches lm
      where lm.id = match_id
        and (lm.player1_id = auth.uid() or lm.player2_id = auth.uid())
    )
  );

create policy "match_results_update_player_or_admin" on public.league_match_results for update
  using (
    entered_by = auth.uid()
    or exists (
      select 1 from public.league_matches lm
      join public.leagues l on l.id = lm.league_id
      where lm.id = match_id and public.is_club_admin(l.club_id)
    )
  );
