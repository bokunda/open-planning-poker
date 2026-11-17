# Integrations: Jira, Slack and Microsoft Teams

Jira and Slack run in the Game Engine and are **off until their secrets are set**. The Microsoft Teams app is a
tab without a bot and needs no secrets. Without them, the app hides the
feature. The **Deploy OPP** workflow passes the secrets to the Game Engine container. After changing them, run
**Deploy OPP** again.

| Integration | Repository secrets | Game Engine settings |
|---|---|---|
| Jira Cloud (OPP-10, 11, 12) | `JIRA_CLIENT_ID`, `JIRA_CLIENT_SECRET` | `Jira__ClientId`, `Jira__ClientSecret` |
| Slack (OPP-22) | `SLACK_CLIENT_ID`, `SLACK_CLIENT_SECRET`, `SLACK_SIGNING_SECRET` | `Chat__SlackClientId`, `Chat__SlackClientSecret`, `Chat__SlackSigningSecret` |
| GitHub (OPP-131) | `GH_TRACKER_CLIENT_ID`, `GH_TRACKER_CLIENT_SECRET` | `GitHub__ClientId`, `GitHub__ClientSecret` |
| Linear (OPP-133) | `LINEAR_CLIENT_ID`, `LINEAR_CLIENT_SECRET` | `Linear__ClientId`, `Linear__ClientSecret` |
| Azure DevOps (OPP-132) | `AZURE_DEVOPS_CLIENT_ID`, `AZURE_DEVOPS_CLIENT_SECRET` | `AzureDevOps__ClientId`, `AzureDevOps__ClientSecret` |

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

## GitHub, Linear and Azure DevOps

The three trackers share one flow (OPP-130): **Import → From GitHub / Linear / Azure DevOps**, connect once, pick a
repository, team or project, preview and import. After the reveal, **Save to KEY** writes the estimate back. Each tab
appears only when its secrets are set. The callback URL is `https://app.openplanningpoker.com/integrations/tracker/<provider>/callback`.

- **GitHub**: create an OAuth app (Settings → Developer settings → OAuth Apps) with the callback
  `…/integrations/tracker/github/callback`. The app asks for the `repo` scope. Issues come from a repository, filtered by
  labels. The estimate is saved as an `estimate: N` label; older `estimate:` labels are removed.
- **Linear**: create an OAuth application (Settings → API → OAuth applications) with the callback
  `…/integrations/tracker/linear/callback` and the scopes `read,write`. Issues come from the team's active cycle (or
  all open issues with the filter `open`). Linear estimates are whole numbers.
- **Azure DevOps**: register an app in Microsoft Entra ID (multi-tenant, web redirect
  `…/integrations/tracker/azure-devops/callback`) with the delegated Azure DevOps permission `user_impersonation`, and
  create a client secret. Work items come from a project, filtered by an iteration path or a WIQL query. The estimate
  goes to Story Points, Effort or Size, whichever the work item type has.

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

The Teams app (OPP-23) is a **tab**, without a bot: it needs no Azure resources and no secrets, and any
organisation can add it. When someone adds the tab to a meeting, a channel or a chat, Teams opens
`https://app.openplanningpoker.com/teams/config`. There they start a new game (they become the host) or paste the
link of a game they already have. The tab then shows the game to everyone, in the side panel or on the meeting
stage, without the site header and footer.

**For a Teams admin of any organisation:**

1. Download `open-planning-poker-teams.zip` from the
   [latest release](https://github.com/bokunda/open-planning-poker/releases/latest/download/open-planning-poker-teams.zip).
2. In the [Teams admin center](https://admin.teams.microsoft.com/), go to **Teams apps → Manage apps → Upload new app**
   and upload the zip. Your app permission policies decide who can use it.
3. People then add it from **Apps → Built for your org**, or with **+** on a meeting, a channel or a chat.

Where the organisation allows custom apps, anyone can also upload the zip for themselves: **Apps → Manage your apps →
Upload an app**.

**For this project:**

- The package is built from [`integrations/teams/`](../integrations/teams/) with
  `integrations/teams/build-package.sh`, and attached to every release. Raise `version` in `manifest.json` when
  the manifest changes; the `id` stays the same.
- nginx must let Teams frame `app.openplanningpoker.com`. Instead of `X-Frame-Options: DENY`, the site sends:

```nginx
add_header Content-Security-Policy "frame-ancestors 'self' https://teams.microsoft.com https://*.teams.microsoft.com https://*.cloud.microsoft https://*.office.com https://*.skype.com" always;
```

Without a bot, Teams does not get the result of each reveal in the chat; the result is in the tab.

## Forge

The Forge issue panel was a spike; see [jira-forge-spike.md](integrations/jira-forge-spike.md).
