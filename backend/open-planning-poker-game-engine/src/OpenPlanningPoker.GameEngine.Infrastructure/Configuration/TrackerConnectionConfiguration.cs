using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class TrackerConnectionConfiguration : IEntityTypeConfiguration<TrackerConnection>
{
    public void Configure(EntityTypeBuilder<TrackerConnection> builder)
    {
        builder.ToTable("tracker_connections");
        builder.HasKey(connection => connection.Id);
        builder.HasIndex(connection => new { connection.PlayerId, connection.Provider }).IsUnique();
        builder.Property(connection => connection.Provider).IsRequired().HasMaxLength(32);
        builder.Property(connection => connection.AccountName).HasMaxLength(255);
        builder.Property(connection => connection.ProtectedAccessToken).IsRequired();
    }
}
