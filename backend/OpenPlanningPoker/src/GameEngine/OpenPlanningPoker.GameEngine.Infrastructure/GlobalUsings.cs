// Global using directives

global using System.Data;
global using OpenPlanningPoker.GameEngine.Application.Abstractions.Clock;
global using OpenPlanningPoker.GameEngine.Application.Exceptions;
global using OpenPlanningPoker.GameEngine.Domain.Abstractions;
global using OpenPlanningPoker.GameEngine.Domain.Identity;
global using OpenPlanningPoker.GameEngine.Domain.Repositories;
global using OpenPlanningPoker.GameEngine.Infrastructure.Clock;
global using OpenPlanningPoker.GameEngine.Infrastructure.Identity;
global using OpenPlanningPoker.GameEngine.Infrastructure.Migrations;
global using OpenPlanningPoker.GameEngine.Infrastructure.Migrations.Seeding;
global using OpenPlanningPoker.GameEngine.Infrastructure.Repositories;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Npgsql;
global using OpenPlanningPoker.GameEngine.Domain.Audits;
global using OpenPlanningPoker.GameEngine.Domain.GamePlayer;
global using OpenPlanningPoker.GameEngine.Domain.Games;
global using OpenPlanningPoker.GameEngine.Domain.GameSettings;
global using OpenPlanningPoker.GameEngine.Domain.Tickets;
global using OpenPlanningPoker.GameEngine.Domain.Votes;
