namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("audits");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Scope).IsRequired();
        builder.Property(audit => audit.ScopeId).IsRequired();
        builder.Property(audit => audit.Action).IsRequired().HasMaxLength(Audit.MaxActionLength);
        builder.Property(audit => audit.Type).IsRequired();
        builder.Property(audit => audit.Detail).HasMaxLength(Audit.MaxDetailLength);
        builder.Property(audit => audit.Value).HasMaxLength(Audit.MaxValueLength);

        // The activity list of one game / team / organization, newest first, and the retention delete.
        builder.HasIndex(audit => new { audit.Scope, audit.ScopeId, audit.CreatedOn });
        builder.HasIndex(audit => audit.CreatedOn);
    }
}
