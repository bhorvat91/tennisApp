-- ============================================================
-- Migration: League third set format
-- ============================================================

-- Add third_set_format column: 'normal' (standard third set) or 'super_tie_break'
alter table public.leagues
  add column if not exists third_set_format text not null default 'normal'
    check (third_set_format in ('normal', 'super_tie_break'));
