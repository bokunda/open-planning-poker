namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.HasKey(game => game.Id);

        builder.Property(game => game.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(game => game.Description)
            .HasMaxLength(4080);

        builder.HasIndex(game => game.TeamId);

        // One-time host link of games started from Slack or Teams (hex SHA-256).
        builder.Property(game => game.HostClaimHash).HasMaxLength(64);

        // Public recap link (OPP-170): looked up by token, so it is unique and indexed.
        builder.Property(game => game.RecapToken).HasMaxLength(32);
        builder.HasIndex(game => game.RecapToken).IsUnique().HasFilter("recap_token IS NOT NULL");

        builder.HasOne(game => game.GameSettings)
            .WithOne(gameSettings => gameSettings.Game);

        builder.HasMany(game => game.GamePlayers)
            .WithOne(gamePlayers => gamePlayers.Game)
            .HasForeignKey(gamePlayer => gamePlayer.GameId);

        builder.HasMany(game => game.Tickets)
            .WithOne(ticket => ticket.Game)
            .HasForeignKey(ticket => ticket.GameId);
    }
}