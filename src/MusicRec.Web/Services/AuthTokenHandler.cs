using System.Net.Http.Headers;

namespace MusicRec.Web.Services;

public sealed class AuthTokenHandler(UserSessionState sessionState) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!sessionState.IsReady)
        {
            try
            {
                await sessionState.InitializeAsync();
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(sessionState.CurrentUser?.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionState.CurrentUser.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
