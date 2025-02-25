namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class GameSettingsConfiguration : IEntityTypeConfiguration<GameSettings>
{
    public void Configure(EntityTypeBuilder<GameSettings> builder)
    {
        builder.ToTable("gamesettings");

        builder.HasKey(gamesettings => gamesettings.Id);

        builder.HasIndex(gamesettings => gamesettings.GameId);
        builder.Property(gamesettings => gamesettings.GameId)
            .IsRequired();

        builder.Property(gamesettings => gamesettings.IsBreakAllowed)
            .IsRequired();

        builder.Property(gamesettings => gamesettings.VotingTime)
            .IsRequired();
    }
}