# Open Planning Poker - Game Engine

This is a Game Engine implementation of Open Planning Poker project.
For mor details visit the main [Open Planning Poker page](https://github.com/bokunda/open-planning-poker).

## Description

The Game Engine implements game mechanics and exposes a GraphQL facade.

## Tech stack
- [.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) - Web Api
- [PostgreSQL](https://www.postgresql.org) - Database
- [Docker](https://www.docker.com/) - Containerisation

### Most important external dependencies
- [HotChocolate](https://chillicream.com/docs/hotchocolate/v15) - GraphQL implementation for .NET
- [Entity Framework](https://learn.microsoft.com/en-us/ef/) - ORM
- [MediatR](https://github.com/jbogard/MediatR) - Mediator implementation in .NET
- [Serilog](https://serilog.net/) - .NET logging with fully-structured events
- [AutoMapper](https://docs.automapper.org/en/stable/Getting-started.html) - object-object mapper
- [FluentValidation](https://docs.fluentvalidation.net/en/latest/) - Library for building strongly-typed validation rules
- [FluentAssertions](https://fluentassertions.com/) - Set of extension methods for assertions in testing to make the assertions more readable and easier to understand.
- [FakeItEasy](https://fakeiteasy.github.io/) - dynamic fake framework for creating all types of fake objects, mocks, stubs etc.
- [Testcontainers.PostgreSql](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/) - Testing library that provides easy and lightweight APIs for bootstrapping integration tests with real services wrapped in Docker containers.
- [NetArchTest.Rules](https://github.com/BenMorris/NetArchTest) - A fluent API for .Net Standard that can enforce architectural rules in unit tests.

## DB Diagram

![DB Diagram](Resources/Database/Open%20Planning%20Poker%20-%20Game%20Engine%20DB%20Schema.png "DB Diagram")

## Technical description

Implementation follows Clean Architecture principles by creating **Api**, **Application**, **Infrastructure**, and **Domain** libs. The idea was to show good practices for organizing the code and handling requests, mappings, validations, errors... 

**Sequential Guid Generator** is implemented so we can guarantee that records will be sorted by Id by default.

A big focus was on tests as well, so the solution contains blueprints for: **Domain tests**, **Application tests**, **API tests (integration tests)**, and **Architecture tests**.

## Requirements
To run this project, the only thing that has to be installed on the machine is [Docker](https://www.docker.com/).

## How To

### Start
Check the [main](https://github.com/bokunda/open-planning-poker) project for details. 

