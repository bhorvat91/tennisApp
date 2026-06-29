-- ============================================================
-- Migration: Extended club membership types and flags
-- ============================================================

-- Add membership_type column: 'member' (regular paid member) or 'guest' (plays only league)
alter table public.club_memberships
  add column if not exists membership_type text not null default 'member'
    check (membership_type in ('member', 'guest'));

-- Add fee_paid flag: tracks whether the member has paid their annual membership fee
alter table public.club_memberships
  add column if not exists fee_paid boolean not null default false;

-- Add can_reserve flag: controls whether the member has the right to make court reservations
alter table public.club_memberships
  add column if not exists can_reserve boolean not null default true;
