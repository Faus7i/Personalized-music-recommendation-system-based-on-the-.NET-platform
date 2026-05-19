using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MusicRec.Web.Services;

public sealed class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly UserSessionState _sessionState;

    public CookieAuthenticationStateProvider(UserSessionState sessionState)
    {
        _sessionState = sessionState;
        _sessionState.Changed += OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var currentUser = _sessionState.CurrentUser;

        if (currentUser is null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(currentUser.Token))
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
            }

            var jwtToken = handler.ReadJwtToken(currentUser.Token);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, currentUser.UserId.ToString()),
                new(ClaimTypes.Name, currentUser.UserName)
            };

            var identity = new ClaimsIdentity(claims, "JWT");
            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(principal));
        }
        catch
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
        }
    }
}
