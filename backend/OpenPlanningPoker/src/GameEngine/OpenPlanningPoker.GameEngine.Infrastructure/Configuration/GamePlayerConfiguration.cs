namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("gameplayer");

        builder.HasKey(gameplayer => new { gameplayer.GameId, gameplayer.PlayerId });
    }
}