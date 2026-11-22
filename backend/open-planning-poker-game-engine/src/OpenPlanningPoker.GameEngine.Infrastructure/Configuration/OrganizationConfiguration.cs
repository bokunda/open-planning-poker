namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(Organization.MaxNameLength);
        builder.Property(o => o.InviteToken).IsRequired().HasMaxLength(32);
    }
}

internal sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("organization_members");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.OrganizationId, m.PlayerId }).IsUnique();
        builder.HasIndex(m => m.PlayerId);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<Organization>().WithMany().HasForeignKey(m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
