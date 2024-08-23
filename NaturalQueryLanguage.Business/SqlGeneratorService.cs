namespace NaturalQueryLanguage.Business;

public class SqlGeneratorService(
    ChatGptClient chatGptClient,
    DbSchemaService dbSchemaService)
{
    public async Task GenerateSql(string userInput, string connectionString)
    {
        string dbSchema;
        await using (var stream = await dbSchemaService.Get(connectionString))
        {
            using var reader = new StreamReader(stream);
            dbSchema = await reader.ReadToEndAsync();
        }

        await chatGptClient.DoRequestAsync(new ChatGptRequest
        {

        });
    }
}
