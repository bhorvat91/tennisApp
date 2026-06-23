-- ============================================================
-- Liga: bodovna pravila — inkrementalna migracija
-- Pokreni jednom u Supabase SQL editoru
-- ============================================================

alter table public.leagues
  add column if not exists points_per_win  int not null default 2 check (points_per_win  in (2, 3)),
  add column if not exists points_per_loss int not null default 1 check (points_per_loss in (0, 1));
