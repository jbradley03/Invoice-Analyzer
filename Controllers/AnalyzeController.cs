using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceAnalyzerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzeController : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Analyze([FromForm] IFormFile file)
    {

        var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

        var endpoint = config["ApiSettings:Endpoint"];
        var apiKey   = config["ApiSettings:ApiKey"];
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (string.IsNullOrEmpty(endpoint))
            return BadRequest("API endpoint is not configured.");

        var credential = new AzureKeyCredential(apiKey);
        var client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

        using var stream = file.OpenReadStream();
        var binaryData = BinaryData.FromStream(stream);

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-invoice",
            binaryData
        );

        var result = operation.Value;
        var extracted = new Dictionary<string, object>();

        if (result.Documents.Count == 0)
            return NotFound("No document found in the analysis result.");

        var doc = result.Documents[0];

        // Extract all top-level fields 
        foreach (var kvp in doc.Fields)
        {
            var name = kvp.Key;
            var field = kvp.Value;
            string displayValue = "[No value]";

            if (field.FieldType == DocumentFieldType.String)
                displayValue = field.ValueString;
            else if (field.FieldType == DocumentFieldType.Int64)
                displayValue = field.ValueInt64?.ToString() ?? "[null]";
            else if (field.FieldType == DocumentFieldType.Double)
                displayValue = field.ValueDouble?.ToString() ?? "[null]";
            else if (field.FieldType == DocumentFieldType.Currency)
                displayValue = $"{field.ValueCurrency.CurrencySymbol}{field.ValueCurrency.Amount}";
            else if (field.FieldType == DocumentFieldType.Date)
                displayValue = field.ValueDate?.ToString("yyyy-MM-dd") ?? "[null]";
            else if (!string.IsNullOrEmpty(field.Content))
                displayValue = field.Content;

            extracted[name] = displayValue;
        }

        // Extract line items from Items field 
        var itemsList = new List<Dictionary<string, object>>();

        if (doc.Fields.TryGetValue("Items", out var itemsField) && itemsField.FieldType == DocumentFieldType.List)
        {
            foreach (var itemField in itemsField.ValueList)
            {
                if (itemField.FieldType == DocumentFieldType.Dictionary)
                {
                    var itemDetails = new Dictionary<string, object>();
                    var itemFields = itemField.ValueDictionary;

                    foreach (var itemKvp in itemFields)
                    {
                        var itemName = itemKvp.Key;
                        var itemVal = itemKvp.Value;

                        string itemDisplayValue = "[No value]";
                        if (itemVal.FieldType == DocumentFieldType.String)
                            itemDisplayValue = itemVal.ValueString;
                        else if (itemVal.FieldType == DocumentFieldType.Int64)
                            itemDisplayValue = itemVal.ValueInt64?.ToString() ?? "[null]";
                        else if (itemVal.FieldType == DocumentFieldType.Double)
                            itemDisplayValue = itemVal.ValueDouble?.ToString() ?? "[null]";
                        else if (itemVal.FieldType == DocumentFieldType.Currency)
                            itemDisplayValue = $"{itemVal.ValueCurrency.CurrencySymbol}{itemVal.ValueCurrency.Amount}" 
                                ?? "[null]";
                        else if (itemVal.FieldType == DocumentFieldType.Date)
                            itemDisplayValue = itemVal.ValueDate?.ToString("yyyy-MM-dd") ?? "[null]";
                        else if (!string.IsNullOrEmpty(itemVal.Content))
                            itemDisplayValue = itemVal.Content;

                        itemDetails[itemName] = itemDisplayValue;
                    }

                    itemsList.Add(itemDetails);
                }
            }
        }

        extracted["Items"] = itemsList;
        extracted["ItemsCount"] = itemsList.Count;

        return Ok(extracted);
    }
}


