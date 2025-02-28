namespace OpenPlanningPoker.UserManagement.GraphQL.Services;

public interface IVocabularyCollectorService
{
    Task<IEnumerable<string>> GetVocabulary(Language language, CancellationToken cancellationToken = default);
}

public class VocabularyCollectorService(
    IConfiguration configuration,
    HttpClient httpClient) : IVocabularyCollectorService
{
    private const int minWordLength = 5;
    public async Task<IEnumerable<string>> GetVocabulary(Language language, CancellationToken cancellationToken = default)
    {
        var url = configuration[$"Vocabulary:{language}:Url"]!;
        var offlineFile = System.IO.Path.Combine("OfflineVocabulary", configuration[$"Vocabulary:{language}:Offline"]!);
        var result = await Get(url, offlineFile, cancellationToken);

        return result;
    }

    private async Task<IEnumerable<string>> Get(string url, string file, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        try { response = await httpClient.GetAsync(url, cancellationToken); } catch { response?.Dispose(); }

        var vocabulary = response?.StatusCode != HttpStatusCode.OK
                ? await File.ReadAllLinesAsync(file, cancellationToken)
                : (await response.Content.ReadAsStringAsync(cancellationToken)).Split("\n");

        return vocabulary
            .Select(x => x.Trim())
            .Where(x => x.Length >= minWordLength)
            .Distinct();

    }
}
