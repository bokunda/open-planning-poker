namespace OpenPlanningPoker.GameEngine.GraphQL.Tests.Schema
{
    
    public class SchemaTests
    {
        [Fact]
        public async Task GenerateSchema()
        {
            // arrange
            var schema = await new ServiceCollection()
                .AddGraphQL()
                .AddTypes()
                .BuildSchemaAsync();

            // act
            var schemaSDL = schema.ToString();

            // assert
            schemaSDL.MatchSnapshot();
        }
        
    }
}
