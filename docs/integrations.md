# Integrations: Jira, Slack and Microsoft Teams

Every integration runs in the Game Engine and is **off until its secrets are set**. Without them, the app hides the
feature. The **Deploy OPP** workflow passes the secrets to the Game Engine container. After changing them, run
**Deploy OPP** again.

| Integration | Repository secrets | Game Engine settings |
|---|---|---|
| Jira Cloud (OPP-10, 11, 12) | `JIRA_CLIENT_ID`, `JIRA_CLIENT_SECRET` | `Jira__ClientId`, `Jira__ClientSecret` |
| Slack (OPP-22) | `SLACK_CLIENT_ID`, `SLACK_CLIENT_SECRET`, `SLACK_SIGNING_SECRET` | `Chat__SlackClientId`, `Chat__SlackClientSecret`, `Chat__SlackSigningSecret` |
| Microsoft Teams (OPP-23) | `TEAMS_APP_ID`, `TEAMS_APP_PASSWORD`, optional `TEAMS_TENANT_ID` | `Chat__TeamsAppId`, `Chat__TeamsAppPassword`, `Chat__TeamsTenantId` |

Tokens (the Jira user tokens and the Slack bot tokens) are encrypted with ASP.NET Core Data Protection, with the keys
in the database (`data_protection_keys`). Only the SHA-256 hash of a chat game's host link is stored.

## Jira Cloud

1. In the [Atlassian developer console](https://developer.atlassian.com/console/myapps/), create an
   **OAuth 2.0 (3LO) integration** with **Resource-level** access.
2. **Permissions → Jira API**: add `read:jira-work`, `write:jira-work` and `read:jira-user`.
3. **Authorization → OAuth 2.0 (3LO)**: set the callback URL to `https://app.openplanningpoker.com/integrations/jira/callback`.
4. **Settings**: copy the Client ID and Secret into `JIRA_CLIENT_ID` and `JIRA_CLIENT_SECRET`.

In the app, the host opens **Import → From Jira**, connects once, and imports issues from a sprint, a board or a JQL
query. After the reveal, **Save to KEY** writes the estimate to the Story Points field.

## Slack

1. Go to [api.slack.com/apps](https://api.slack.com/apps) and choose **Create New App → From a manifest**. Paste
   [`integrations/slack/manifest.yml`](../integrations/slack/manifest.yml).
2. From **Basic Information**, copy the Client ID, Client Secret and Signing Secret into the three `SLACK_*`
   secrets, then run **Deploy OPP**.
3. Install the app in a workspace at `https://ge.openplanningpoker.com/integrations/slack/install`. You can share
   the same link as the public install link.

In a channel, type `/poker Sprint 43`. Open Planning Poker posts the room with a **Join** button, and only the
person who ran the command gets the **Open as host** link. After each reveal, the result is posted in the thread.
In a private channel, add the app first with `/invite @Open Planning Poker`.

## Microsoft Teams

1. In the [Teams Developer Portal](https://dev.teams.microsoft.com/) (or the Azure portal), create a **bot**.
   - Set its messaging endpoint to `https://ge.openplanningpoker.com/integrations/teams/messages`.
   - Create a client secret.
   - Put the app (client) ID in `TEAMS_APP_ID` and the secret in `TEAMS_APP_PASSWORD`.
   - For a single-tenant bot, also set `TEAMS_TENANT_ID`.
2. Build the app package from [`integrations/teams/`](../integrations/teams/):
   - replace `{{TEAMS_APP_ID}}` in `manifest.json`;
   - zip `manifest.json`, `color.png` and `outline.png`;
   - upload the zip in Teams (**Apps → Manage your apps → Upload an app**) or in the Developer Portal.
3. Run **Deploy OPP**.

**Allow the app in Teams frames.** The meeting tab shows `app.openplanningpoker.com` inside Teams, so its nginx site
must allow Teams as a frame ancestor. If the site sends `X-Frame-Options: DENY` / `SAMEORIGIN`, replace it with:

```nginx
add_header Content-Security-Policy "frame-ancestors 'self' https://teams.microsoft.com https://*.teams.microsoft.com https://*.cloud.microsoft https://*.office.com" always;
```

In a chat, use the **Start planning poker** message extension. It shares a card with a **Join** button. Teams cannot
show a link to one person only here, so the first person who opens the room becomes the host, and they can hand
the role over. After each reveal, the result is posted to the chat. In a meeting, add **Open Planning Poker** as a
tab and paste a room link (or leave it empty for a new room).

## Forge

The Forge issue panel was a spike; see [jira-forge-spike.md](integrations/jira-forge-spike.md).
