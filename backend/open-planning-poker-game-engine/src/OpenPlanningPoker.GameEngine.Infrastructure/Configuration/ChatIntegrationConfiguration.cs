using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class SlackInstallationConfiguration : IEntityTypeConfiguration<SlackInstallation>
{
    public void Configure(EntityTypeBuilder<SlackInstallation> builder)
    {
        builder.ToTable("slack_installations");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.WorkspaceId).IsUnique();
        builder.Property(item => item.WorkspaceId).IsRequired().HasMaxLength(32);
        builder.Property(item => item.WorkspaceName).HasMaxLength(255);
        builder.Property(item => item.ProtectedBotToken).IsRequired();
    }
}

internal sealed class ChatPostConfiguration : IEntityTypeConfiguration<ChatPost>
{
    public void Configure(EntityTypeBuilder<ChatPost> builder)
    {
        builder.ToTable("chat_posts");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.GameId).IsUnique();
        builder.Property(item => item.Provider).IsRequired().HasMaxLength(16);
        builder.Property(item => item.WorkspaceId).IsRequired().HasMaxLength(64);
        builder.Property(item => item.ChannelId).IsRequired().HasMaxLength(512);
        builder.Property(item => item.MessageId).HasMaxLength(64);
        builder.Property(item => item.ServiceUrl).HasMaxLength(512);
    }
}
