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