namespace OpenPlanningPoker.UserManagement.GraphQL.Tests.Schema;


public class SchemaTests
{
    [Fact]
    public async Task GenerateSchema()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorization()
            .AddTypes()
            .BuildSchemaAsync();

        // act
        var schemaSDL = schema.ToString();

        // assert
        schemaSDL.MatchSnapshot();
    }
    
}
