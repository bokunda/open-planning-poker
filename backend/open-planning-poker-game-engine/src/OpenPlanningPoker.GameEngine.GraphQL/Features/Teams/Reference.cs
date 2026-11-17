namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>An estimated ticket from one of the team's sprints.</summary>
public sealed class TeamTicket
{
    public Guid TicketId { get; set; }
    public Guid GameId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public DateTimeOffset SprintDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExternalKey { get; set; }
    public string? ExternalUrl { get; set; }

    /// <summary>The final estimate, or the most common card.</summary>
    public string Estimate { get; set; } = string.Empty;

    /// <summary>Consensus, Close or Wide (as in the report).</summary>
    public string Spread { get; set; } = string.Empty;

    public static TeamTicket From(Application.Features.Teams.TeamTicket t) => new()
    {
        TicketId = t.TicketId, GameId = t.GameId, SprintName = t.SprintName, SprintDate = t.SprintDate, Name = t.Name,
        Description = t.Description, ExternalKey = t.ExternalKey, ExternalUrl = t.ExternalUrl, Estimate = t.Estimate, Spread = t.Spread,
    };
}

/// <summary>A reference ticket of the team: what a card value looks like for this team (OPP-127).</summary>
public sealed class TeamAnchor
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Note { get; set; }
    public TeamTicket Ticket { get; set; } = new();

    public static TeamAnchor From(TeamAnchorResponse a) => new() { Id = a.Id, Value = a.Value, Note = a.Note, Ticket = TeamTicket.From(a.Ticket) };
}

public sealed class TeamHistory
{
    public List<TeamTicket> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

/// <summary>Anchors and similar past tickets for the ticket being voted on (OPP-128). Empty outside team rooms.</summary>
public sealed class TicketReference
{
    public Guid? TeamId { get; set; }
    public List<TeamAnchor> Anchors { get; set; } = [];
    public List<TeamTicket> Similar { get; set; } = [];
}

[ExtendObjectType(typeof(Team))]
public sealed class TeamReferenceFields
{
    /// <summary>Reference tickets, by card value. Empty for non-members.</summary>
    public async Task<List<TeamAnchor>> GetAnchorsAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] ISender sender, CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureTeamMemberAsync(team.Id, cancellationToken) is not null) return [];
        return [.. (await sender.Send(new GetTeamAnchorsQuery(team.Id), cancellationToken)).Select(TeamAnchor.From)];
    }

    /// <summary>Estimated tickets of all sprints, newest first, filtered by text and card value. Empty for non-members.</summary>
    public async Task<TeamHistory> GetHistoryAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] ISender sender,
        string? search, string? value, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamMemberAsync(team.Id, cancellationToken) is not null) return new TeamHistory();
        var page = await sender.Send(new SearchTeamHistoryQuery(team.Id, search, value, skip, take), cancellationToken);
        return new TeamHistory { Items = [.. page.Items.Select(TeamTicket.From)], TotalCount = page.TotalCount };
    }
}

[QueryType]
public class ReferenceQueries
{
    /// <summary>The team's anchors and up to three similar past tickets for a ticket. Players of the game only.</summary>
    public async Task<FieldResult<TicketReference, ApplicationError>> GetTicketReferenceAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureMemberOfTicketAsync(ticketId, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetTicketReferenceQuery(ticketId), cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        var r = result.Value!;
        return new TicketReference { TeamId = r.TeamId, Anchors = [.. r.Anchors.Select(TeamAnchor.From)], Similar = [.. r.Similar.Select(TeamTicket.From)] };
    }
}

[MutationType]
public class ReferenceMutations
{
    /// <summary>Pins an estimated ticket of the team's sprints as the reference for its card (two per card). Team members.</summary>
    public async Task<FieldResult<TeamAnchor, ApplicationError>> PinTeamAnchorAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid ticketId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamMemberAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new PinAnchorCommand(teamId, ticketId, note), cancellationToken);
        return result.IsSuccess ? TeamAnchor.From(result.Value!) : result.Error!;
    }

    /// <summary>Removes a reference ticket. Team members.</summary>
    public async Task<FieldResult<bool, ApplicationError>> UnpinTeamAnchorAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid teamId,
        [Required] Guid anchorId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamMemberAsync(teamId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new UnpinAnchorCommand(teamId, anchorId), cancellationToken);
        return result.IsSuccess ? result.Value : result.Error!;
    }
}
