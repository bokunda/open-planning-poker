namespace OpenPlanningPoker.GameEngine.Infrastructure.Configuration;

internal sealed class RetroConfiguration : IEntityTypeConfiguration<Retro>
{
    public void Configure(EntityTypeBuilder<Retro> builder)
    {
        builder.ToTable("retros");

        builder.HasKey(retro => retro.Id);

        // One retro per sprint.
        builder.HasIndex(retro => retro.GameId).IsUnique();
        builder.HasIndex(retro => retro.TeamId);

        builder.Property(retro => retro.Template)
            .IsRequired()
            .HasMaxLength(Retro.MaxTemplateLength);

        builder.Property(retro => retro.Phase)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Ignore(retro => retro.Columns);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(retro => retro.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(retro => retro.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RetroCardConfiguration : IEntityTypeConfiguration<RetroCard>
{
    public void Configure(EntityTypeBuilder<RetroCard> builder)
    {
        builder.ToTable("retro_cards");

        builder.HasKey(card => card.Id);

        builder.HasIndex(card => card.RetroId);

        builder.Property(card => card.Text)
            .IsRequired()
            .HasMaxLength(RetroCard.MaxTextLength);

        builder.Property(card => card.GroupTitle)
            .HasMaxLength(RetroCard.MaxGroupTitleLength);

        builder.HasOne<Retro>()
            .WithMany()
            .HasForeignKey(card => card.RetroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RetroVoteConfiguration : IEntityTypeConfiguration<RetroVote>
{
    public void Configure(EntityTypeBuilder<RetroVote> builder)
    {
        builder.ToTable("retro_votes");

        builder.HasKey(vote => vote.Id);

        builder.HasIndex(vote => new { vote.RetroId, vote.PlayerId });

        builder.HasOne<Retro>()
            .WithMany()
            .HasForeignKey(vote => vote.RetroId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RetroCard>()
            .WithMany()
            .HasForeignKey(vote => vote.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RetroActionItemConfiguration : IEntityTypeConfiguration<RetroActionItem>
{
    public void Configure(EntityTypeBuilder<RetroActionItem> builder)
    {
        builder.ToTable("retro_action_items");

        builder.HasKey(item => item.Id);

        builder.HasIndex(item => new { item.TeamId, item.Done });

        builder.Property(item => item.Text)
            .IsRequired()
            .HasMaxLength(RetroActionItem.MaxTextLength);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(item => item.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // The card may be deleted later; the action item stays.
        builder.HasOne<RetroCard>()
            .WithMany()
            .HasForeignKey(item => item.CardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
