using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TennisHub.Shared.Configuration;
using TennisHub.Shared.Services.Auth;
using TennisHub.Shared.Services.BookingRules;
using TennisHub.Shared.Services.Clubs;
using TennisHub.Shared.Services.Courts;
using TennisHub.Shared.Services.Memberships;
using TennisHub.Shared.Services.Leagues;
using TennisHub.Shared.Services.Reservations;

namespace TennisHub.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTennisHubShared(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new SupabaseSettings();
        configuration.GetSection(SupabaseSettings.SectionName).Bind(settings);

        services.AddSingleton(settings);
        services.AddSingleton(_ => new Supabase.Client(settings.Url, settings.AnonKey, new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        }));

        services.AddAuthorizationCore();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<SupabaseAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SupabaseAuthenticationStateProvider>());

        services.AddScoped<IClubService, ClubService>();
        services.AddScoped<ICourtService, CourtService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IBookingRuleService, BookingRuleService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ILeagueService, LeagueService>();

        return services;
    }
}
