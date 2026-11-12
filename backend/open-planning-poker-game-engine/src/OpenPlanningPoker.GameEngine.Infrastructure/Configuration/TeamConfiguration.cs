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
