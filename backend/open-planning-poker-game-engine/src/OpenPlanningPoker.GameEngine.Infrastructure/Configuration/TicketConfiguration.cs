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

        builder.Property(ticket => ticket.ClarityNotes)
            .HasMaxLength(Ticket.MaxClarityLength);

        builder.Property(ticket => ticket.DiscussionSummary)
            .HasMaxLength(Ticket.MaxSummaryLength);

        builder.Property(ticket => ticket.FinalEstimate)
            .HasMaxLength(Ticket.MaxFinalEstimateLength);

        builder.Property(ticket => ticket.ExternalKey)
            .HasMaxLength(Ticket.MaxExternalKeyLength);

        builder.Property(ticket => ticket.ExternalUrl)
            .HasMaxLength(Ticket.MaxExternalUrlLength);

        builder.Property(ticket => ticket.SortColumn)
            .HasMaxLength(Ticket.MaxFinalEstimateLength);

        builder.HasMany(ticket => ticket.Votes)
            .WithOne(vote => vote.Ticket)
            .HasForeignKey(ticket => ticket.TicketId);
    }
}

internal sealed class TicketCycleTimeConfiguration : IEntityTypeConfiguration<TicketCycleTime>
{
    public void Configure(EntityTypeBuilder<TicketCycleTime> builder)
    {
        builder.ToTable("ticket_cycle_times");
        builder.HasKey(c => c.TicketId);
        builder.Ignore(c => c.Days);
        builder.HasOne<Ticket>().WithMany().HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}
