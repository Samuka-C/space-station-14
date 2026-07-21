using Robust.Server;

namespace Content.Server.Discord.WebhookMessages;

public sealed partial class EventWebhook : IPostInjectInit
{
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IBaseServer _baseServer = default!;

    private const string SawmillDiscordName = "discord";
    private ISawmill _sawmill = default!;

    void IPostInjectInit.PostInject()
    {
        _sawmill = Logger.GetSawmill(SawmillDiscordName);
    }

    public void TrySendMessage(string adminUsername, int roundId, string eventDescription, LocId? categoryTitle, string? webhookUrl = null)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            return;

        var serverName = _baseServer.ServerName;

        var payload = new WebhookPayload()
        {
            Username = categoryTitle == null
                ? Loc.GetString("event-log-webhook-title")
                : Loc.GetString("event-log-webhook-title-with-category", ("category", Loc.GetString(categoryTitle))),
            Embeds = new List<WebhookEmbed>()
            {
                new()
                {
                    Title = adminUsername,
                    // Gotta remove the alpha channel so discord doesn't freak out
                    Color = Color.DarkViolet.ToArgb() & 0x00FFFFFF, //#9400D3
                    Description = eventDescription,
                    Footer = new WebhookEmbedFooter()
                    {
                        Text = Loc.GetString(
                            "event-log-webhook-footer",
                            ("serverName", serverName),
                            ("roundId", roundId)),
                    },
                },
            },
        };

        CreateWebhookMessage(webhookUrl, payload);
    }

    private async void CreateWebhookMessage(string webhookUrl, WebhookPayload payload)
    {
        try
        {
            if (await _discord.GetWebhook(webhookUrl) is not {} identifier)
                return;

            await _discord.CreateMessage(identifier.ToIdentifier(), payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending vote webhook to Discord: {e}");
        }
    }
}
