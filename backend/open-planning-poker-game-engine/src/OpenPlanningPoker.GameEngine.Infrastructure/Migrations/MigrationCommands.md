# OpenPlanningPoker.GameEngine Entity Framework Code-First Migration Commands

Migration commands for OpenPlanningPoker.GameEngine solution.

## Define environment where migration should be executed
$env:ASPNETCORE_ENVIRONMENT='DEV'

## Add new migration
`Add-Migration -Project OpenPlanningPoker.GameEngine.Infrastructure -Name TODO -Context OpenPlanningPokerGameEngineDbContext -StartUpProject OpenPlanningPoker.GameEngine.GraphQL` 

## Update database
`Update-Database -Project OpenPlanningPoker.GameEngine.Infrastructure -Context OpenPlanningPokerGameEngineDbContext -StartUpProject OpenPlanningPoker.GameEngine.GraphQL` 

## Debbuger
if (System.Diagnostics.Debugger.IsAttached == false) System.Diagnostics.Debugger.Launch();

## EF Core 6 - history 
https://devblogs.microsoft.com/dotnet/prime-your-flux-capacitor-sql-server-temporal-tables-in-ef-core-6-0/