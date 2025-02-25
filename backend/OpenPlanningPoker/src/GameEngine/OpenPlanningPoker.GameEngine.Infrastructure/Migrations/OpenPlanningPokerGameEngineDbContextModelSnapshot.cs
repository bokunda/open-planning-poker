﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OpenPlanningPoker.GameEngine.Infrastructure;

#nullable disable

namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations
{
    [DbContext(typeof(OpenPlanningPokerGameEngineDbContext))]
    partial class OpenPlanningPokerGameEngineDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Audits.Audit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(4080)
                        .HasColumnType("character varying(4080)")
                        .HasColumnName("description");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("name");

                    b.Property<Guid>("ObjectId")
                        .HasColumnType("uuid")
                        .HasColumnName("object_id");

                    b.Property<int>("ObjectType")
                        .HasColumnType("integer")
                        .HasColumnName("object_type");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_audits");

                    b.HasIndex("ObjectId")
                        .HasDatabaseName("ix_audits_object_id");

                    b.HasIndex("ObjectType")
                        .HasDatabaseName("ix_audits_object_type");

                    b.HasIndex("Type")
                        .HasDatabaseName("ix_audits_type");

                    b.ToTable("audits", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.GamePlayer.GamePlayer", b =>
                {
                    b.Property<Guid>("GameId")
                        .HasColumnType("uuid")
                        .HasColumnName("game_id");

                    b.Property<Guid>("PlayerId")
                        .HasColumnType("uuid")
                        .HasColumnName("player_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.HasKey("GameId", "PlayerId")
                        .HasName("pk_gameplayer");

                    b.ToTable("gameplayer", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.GameSettings.GameSettings", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<Guid>("GameId")
                        .HasColumnType("uuid")
                        .HasColumnName("game_id");

                    b.Property<bool>("IsBreakAllowed")
                        .HasColumnType("boolean")
                        .HasColumnName("is_break_allowed");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<int>("VotingTime")
                        .HasColumnType("integer")
                        .HasColumnName("voting_time");

                    b.HasKey("Id")
                        .HasName("pk_gamesettings");

                    b.HasIndex("GameId")
                        .IsUnique()
                        .HasDatabaseName("ix_gamesettings_game_id");

                    b.ToTable("gamesettings", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Games.Game", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(4080)
                        .HasColumnType("character varying(4080)")
                        .HasColumnName("description");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_games");

                    b.ToTable("games", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Tickets.Ticket", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(4080)
                        .HasColumnType("character varying(4080)")
                        .HasColumnName("description");

                    b.Property<Guid>("GameId")
                        .HasColumnType("uuid")
                        .HasColumnName("game_id");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_tickets");

                    b.HasIndex("GameId")
                        .HasDatabaseName("ix_tickets_game_id");

                    b.ToTable("tickets", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Votes.Vote", b =>
                {
                    b.Property<Guid>("PlayerId")
                        .HasColumnType("uuid")
                        .HasColumnName("player_id");

                    b.Property<Guid>("TicketId")
                        .HasColumnType("uuid")
                        .HasColumnName("ticket_id");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_deleted");

                    b.Property<int>("Value")
                        .HasColumnType("integer")
                        .HasColumnName("value");

                    b.HasKey("PlayerId", "TicketId")
                        .HasName("pk_votes");

                    b.HasIndex("Id")
                        .HasDatabaseName("ix_votes_id");

                    b.HasIndex("TicketId")
                        .HasDatabaseName("ix_votes_ticket_id");

                    b.ToTable("votes", (string)null);
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.GamePlayer.GamePlayer", b =>
                {
                    b.HasOne("OpenPlanningPoker.GameEngine.Domain.Games.Game", "Game")
                        .WithMany("GamePlayers")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_gameplayer_games_game_id");

                    b.Navigation("Game");
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.GameSettings.GameSettings", b =>
                {
                    b.HasOne("OpenPlanningPoker.GameEngine.Domain.Games.Game", "Game")
                        .WithOne("GameSettings")
                        .HasForeignKey("OpenPlanningPoker.GameEngine.Domain.GameSettings.GameSettings", "GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_gamesettings_games_game_id");

                    b.Navigation("Game");
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Tickets.Ticket", b =>
                {
                    b.HasOne("OpenPlanningPoker.GameEngine.Domain.Games.Game", "Game")
                        .WithMany("Tickets")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_tickets_games_game_id");

                    b.Navigation("Game");
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Votes.Vote", b =>
                {
                    b.HasOne("OpenPlanningPoker.GameEngine.Domain.Tickets.Ticket", "Ticket")
                        .WithMany("Votes")
                        .HasForeignKey("TicketId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_votes_tickets_ticket_id");

                    b.Navigation("Ticket");
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Games.Game", b =>
                {
                    b.Navigation("GamePlayers");

                    b.Navigation("GameSettings")
                        .IsRequired();

                    b.Navigation("Tickets");
                });

            modelBuilder.Entity("OpenPlanningPoker.GameEngine.Domain.Tickets.Ticket", b =>
                {
                    b.Navigation("Votes");
                });
#pragma warning restore 612, 618
        }
    }
}
