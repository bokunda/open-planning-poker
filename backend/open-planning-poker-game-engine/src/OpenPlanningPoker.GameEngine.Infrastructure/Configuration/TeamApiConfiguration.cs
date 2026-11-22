namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class TeamApiTokenConfiguration : IEntityTypeConfiguration<TeamApiToken>
{
    public void Configure(EntityTypeBuilder<TeamApiToken> builder)
    {
        builder.ToTable("team_api_tokens");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.Hash).IsUnique();
        builder.HasIndex(t => t.TeamId);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(TeamApiToken.MaxNameLength);
        builder.Property(t => t.Hash).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Prefix).IsRequired().HasMaxLength(16);
        builder.Ignore(t => t.IsActive);
        builder.HasOne<Team>().WithMany().HasForeignKey(t => t.TeamId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TeamWebhookConfiguration : IEntityTypeConfiguration<TeamWebhook>
{
    public void Configure(EntityTypeBuilder<TeamWebhook> builder)
    {
        builder.ToTable("team_webhooks");
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.TeamId);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(TeamWebhook.MaxUrlLength);
        builder.Property(w => w.ProtectedSecret).IsRequired().HasMaxLength(1024);
        builder.Property(w => w.EventList).IsRequired().HasMaxLength(128);
        builder.Ignore(w => w.SubscribedEvents);
        builder.HasOne<Team>().WithMany().HasForeignKey(w => w.TeamId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TeamWebhookDeliveryConfiguration : IEntityTypeConfiguration<TeamWebhookDelivery>
{
    public void Configure(EntityTypeBuilder<TeamWebhookDelivery> builder)
    {
        builder.ToTable("team_webhook_deliveries");
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.WebhookId, d.CreatedOn });
        builder.Property(d => d.Event).IsRequired().HasMaxLength(32);
        builder.Property(d => d.Error).HasMaxLength(TeamWebhookDelivery.MaxErrorLength);
        builder.HasOne<TeamWebhook>().WithMany().HasForeignKey(d => d.WebhookId).OnDelete(DeleteBehavior.Cascade);
    }
}
