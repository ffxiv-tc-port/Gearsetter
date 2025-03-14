using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Gearsetter.DataSources.GoogleDrive;

public sealed class SheetExporter
{
    private const string DataSheet = "1hEj9KCDv0TT1NiGJ0S7afS4hfGMPb6tetqXQetYETUE";
    private const int ItemSheetId = 557462166;
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        GoogleCredential credential = GoogleCredential.FromFile("api_credentials.json")
            .CreateScoped(SheetsService.Scope.Spreadsheets);
        Console.WriteLine($"Credentials: {credential}");
        using var sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Gearsetter",
        });

        var root = await sheetsService.Spreadsheets.GetByDataFilter(new GetSpreadsheetByDataFilterRequest
        {
            /*
            DataFilters =
            [
                new DataFilter
                {
                    GridRange = new GridRange
                    {
                        SheetId = ItemSheetId,
                    }
                }
            ]*/
            IncludeGridData = true,
        }, DataSheet).ExecuteAsync(CancellationTokenSource.Token);

        Console.WriteLine($"Root: {root}");
        var sheet = root.Sheets.Single(x => x.Properties.SheetId == ItemSheetId);
        Console.WriteLine($"Sheet: {sheet}");

        Dictionary<string, List<string>> itemSources = [];
        foreach (var row in sheet.Data[0].RowData)
        {
            string? category = row.Values[1].UserEnteredValue?.StringValue;
            if (category == null)
                break;

            if (category != "Loot")
                continue;

            string itemName = row.Values[0].UserEnteredValue.StringValue;
            List<string> sources = row.Values.Skip(2).Select(x => x.UserEnteredValue?.StringValue)
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .ToList();
            if (sources.Count == 0)
                continue;

            if (itemSources.TryGetValue(itemName, out var existingSources))
                existingSources.AddRange(sources);
            else
                itemSources.Add(itemName, sources);
        }

        string json = JsonSerializer.Serialize(itemSources, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        await File.WriteAllTextAsync("../../../../Gearsetter.DataProcessor/itemSources.json", json);
    }
}
