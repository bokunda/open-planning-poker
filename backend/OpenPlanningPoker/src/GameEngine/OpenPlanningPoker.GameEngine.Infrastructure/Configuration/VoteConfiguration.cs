namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("votes");

        builder.HasKey(vote => new { vote.PlayerId, vote.TicketId });

        builder.HasIndex(vote => vote.Id);

        builder.HasOne(vote => vote.Ticket)
            .WithMany(ticket => ticket.Votes)
            .HasForeignKey(vote => vote.TicketId);
    }
}