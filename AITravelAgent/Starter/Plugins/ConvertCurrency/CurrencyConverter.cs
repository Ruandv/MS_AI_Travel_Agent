using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AITravelAgent;

public class CurrencyConverter
{
    public required string Name { get; set; }
    public double UnitsPerUSD { get; set; }
    public double USDPerUnit { get; set; }

    // Use a static constructor to ensure dictionary initialization
    static CurrencyConverter()
    {
        currencyDictionary = [];
    }

    private static Dictionary<string, CurrencyConverter> currencyDictionary;

    public static Dictionary<string, CurrencyConverter> Currencies
    {
        get
        {
            if (currencyDictionary.Count == 0)
            {
                InitializeCurrencies();
            }

            return currencyDictionary;
        }
    }

    public static void InitializeCurrencies()
    {
        currencyDictionary = [];

        string filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Plugins\\ConvertCurrency\\currencies.txt"
        );

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Couldn't find currencies.txt at {filePath}"
            );
        }

        using StreamReader reader = new(filePath);

        // Skip the header row
        reader.ReadLine();

        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine()!;
            string[] columns = line.Split('\t');

            if (columns.Length == 4)
            {
                // Create a Currency object
                CurrencyConverter currency = new()
                {
                    Name = columns[1],
                    UnitsPerUSD = double.Parse(columns[2]),
                    USDPerUnit = double.Parse(columns[3])
                };

                // Add the currency to the dictionary
                currencyDictionary.Add(columns[0], currency);
            }
        }
    }


    [KernelFunction,
    Description("Convert an amount from one currency to another")]
    public static string ConvertAmount(
        [Description("The base currency code that the amount must be converted from")] string baseCurrencyCode,
        [Description("The target currency code that the amount must be converted to")] string targetCurrencyCode,
        [Description("The amount that we should convert")] double amount
        )
    {
        var currencyDictionary = CurrencyConverter.Currencies;
        var baseCurrency = currencyDictionary[baseCurrencyCode];
        var targetCurrency = currencyDictionary[targetCurrencyCode];
        double convertedAmount = amount * baseCurrency.USDPerUnit;
        double targetAmount = convertedAmount * targetCurrency.UnitsPerUSD;
        return @$"${amount} {baseCurrencyCode} is approximately 
            {targetAmount.ToString("C")} in {targetCurrency.Name}s ({targetCurrencyCode})";
    }
}