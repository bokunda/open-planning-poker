namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.HasIndex(ticket => ticket.GameId);

        builder.Property(ticket => ticket.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(ticket => ticket.Description)
            .HasMaxLength(4080);

        builder.HasMany(ticket => ticket.Votes)
            .WithOne(vote => vote.Ticket)
            .HasForeignKey(ticket => ticket.TicketId);
    }
}