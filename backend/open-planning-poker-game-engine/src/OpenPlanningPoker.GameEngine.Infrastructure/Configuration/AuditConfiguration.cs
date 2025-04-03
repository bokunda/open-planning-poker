namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("audits");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(audit => audit.Description)
            .HasMaxLength(4080);

        builder.Property(audit => audit.Type)
            .IsRequired();

        builder.Property(audit => audit.ObjectType)
            .IsRequired();

        builder.Property(audit => audit.ObjectId)
            .IsRequired();

        builder.HasIndex(audit => audit.Type);
        builder.HasIndex(audit => audit.ObjectType);
        builder.HasIndex(audit => audit.ObjectId);
    }
}
