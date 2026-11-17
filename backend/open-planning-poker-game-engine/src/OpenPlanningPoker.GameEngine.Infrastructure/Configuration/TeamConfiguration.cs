namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(team => team.Id);

        builder.Property(team => team.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(team => team.Description)
            .HasMaxLength(4080);

        builder.Property(team => team.SummaryWebhookUrl)
            .HasMaxLength(Team.MaxWebhookLength);

        builder.HasMany(team => team.Members)
            .WithOne(member => member.Team)
            .HasForeignKey(member => member.TeamId);
    }
}

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");

        builder.HasKey(member => member.Id);

        builder.HasIndex(member => new { member.TeamId, member.PlayerId }).IsUnique();
        builder.HasIndex(member => member.PlayerId);
    }
}

internal sealed class TeamAnchorConfiguration : IEntityTypeConfiguration<TeamAnchor>
{
    public void Configure(EntityTypeBuilder<TeamAnchor> builder)
    {
        builder.ToTable("team_anchors");

        builder.HasKey(anchor => anchor.Id);

        builder.HasIndex(anchor => new { anchor.TeamId, anchor.TicketId }).IsUnique();

        builder.Property(anchor => anchor.Value)
            .IsRequired()
            .HasMaxLength(Ticket.MaxFinalEstimateLength);

        builder.Property(anchor => anchor.Note)
            .HasMaxLength(TeamAnchor.MaxNoteLength);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(anchor => anchor.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(anchor => anchor.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
