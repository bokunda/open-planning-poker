namespace OpenPlanningPoker.GameEngine.Infrastructure.Migrations;

public static class DbInitializer
{
    private static readonly IList<ISeeding> SeedingItems = new List<ISeeding>(10);

    static DbInitializer()
    {
        // TODO: Add seedings here...
    }

    // EF Core way of seeding data: https://docs.microsoft.com/en-us/ef/core/modeling/data-seeding
    public static void SeedData(ModelBuilder modelBuilder)
    {
        foreach (var seeding in SeedingItems)
        {
            seeding.Seed(modelBuilder);
        }
    }
}