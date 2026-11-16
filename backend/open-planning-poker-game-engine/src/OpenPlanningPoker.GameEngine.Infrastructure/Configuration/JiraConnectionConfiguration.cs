using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class JiraConnectionConfiguration : IEntityTypeConfiguration<JiraConnection>
{
    public void Configure(EntityTypeBuilder<JiraConnection> builder)
    {
        builder.ToTable("jira_connections");
        builder.HasKey(connection => connection.Id);
        builder.HasIndex(connection => connection.PlayerId).IsUnique();
        builder.Property(connection => connection.CloudId).HasMaxLength(64);
        builder.Property(connection => connection.SiteName).HasMaxLength(255);
        builder.Property(connection => connection.SiteUrl).HasMaxLength(512);
        builder.Property(connection => connection.ProtectedAccessToken).IsRequired();
        builder.Property(connection => connection.ProtectedRefreshToken).IsRequired();
    }
}
