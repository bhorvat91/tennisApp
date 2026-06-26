-- Add extended player profile fields
alter table public.profiles
  add column if not exists default_club_id uuid references public.clubs(id) on delete set null,
  add column if not exists avatar_url text,
  add column if not exists racket text;
