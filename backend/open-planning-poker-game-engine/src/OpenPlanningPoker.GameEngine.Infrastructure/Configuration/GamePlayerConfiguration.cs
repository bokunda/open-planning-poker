namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("gameplayer");

        builder.HasIndex(gameplayer => new { gameplayer.GameId });
        builder.HasIndex(gameplayer => new { gameplayer.PlayerId });

        builder.HasKey(gameplayer => new { gameplayer.Id });
    }
}