namespace OpenPlanningPoker.UserManagement.GraphQL.Services;

public interface IUsernameGeneratorService
{
    public Task<string> GenerateUsername(Language language, CancellationToken cancellationToken = default);
}

public class UsernameGeneratorService(IVocabularyCollectorService vocabularyCollectorService) : IUsernameGeneratorService
{
    public async Task<string> GenerateUsername(Language language, CancellationToken cancellationToken = default)
    {
        var vocabulary = await vocabularyCollectorService.GetVocabulary(language, cancellationToken);
        var firstWord = GetRandomVocabularyItem(vocabulary);
        var secondWord = GetRandomVocabularyItem(vocabulary);
        return $"{firstWord} {secondWord}";
    }

    private static string GetRandomVocabularyItem(IEnumerable<string> vocabulary)
    {
        var item = vocabulary.ElementAt(new Random().Next(vocabulary.Count()));
        return item.Length > 2 ? $"{char.ToUpper(item.First())}{item[1..]}" : string.Empty;
    }
}
