-- Allow club admins to see profiles of pending membership users
-- so that the admin dashboard shows player names instead of IDs.

drop policy if exists "profiles_select" on public.profiles;

create policy "profiles_select" on public.profiles for select
using (
  id = auth.uid()
  or exists (
    select 1 from public.club_memberships cm1
    join public.club_memberships cm2 on cm1.club_id = cm2.club_id
    where cm1.user_id = auth.uid() and cm1.status = 'approved'
      and cm2.user_id = profiles.id and cm2.status = 'approved'
  )
  or exists (
    select 1 from public.club_memberships cm
    where cm.user_id = profiles.id
      and public.is_club_admin(cm.club_id)
  )
);
