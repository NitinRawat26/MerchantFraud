using System.Globalization;
using System.Text.Json;
using MerchantFraud.Core.Domain;

namespace MerchantFraud.Ingestion;

public static class TransactionFeedParser
{
    public static TransactionFeedResult ParseJson(string json, TransactionIngestionOptions? options = null)
    {
        options ??= new TransactionIngestionOptions();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        JsonElement array;

        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
        }
        else if (root.ValueKind == JsonValueKind.Object && TryGetJsonProperty(root, "transactions", out var transactionsElement) && transactionsElement.ValueKind == JsonValueKind.Array)
        {
            array = transactionsElement;
        }
        else
        {
            throw new FormatException("JSON feed must be an array or an object with a transactions array.");
        }

        var transactions = new List<FraudTransaction>();
        var errors = new List<string>();
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            try
            {
                transactions.Add(ParseJsonElement(item, index, options));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {index}: {ex.Message}");
            }
            index++;
        }

        return new TransactionFeedResult(transactions, errors);
    }

    public static TransactionFeedResult ParseCsv(string csv, TransactionIngestionOptions? options = null)
    {
        options ??= new TransactionIngestionOptions();
        var rows = CsvReader.Parse(csv);
        if (rows.Count == 0)
        {
            return new TransactionFeedResult(Array.Empty<FraudTransaction>(), new[] { "CSV feed is empty." });
        }

        var headers = rows[0].Select(NormalizeToken).ToArray();
        var transactions = new List<FraudTransaction>();
        var errors = new List<string>();

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var fields = new Dictionary<string, string?>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++)
            {
                fields[headers[i]] = i < row.Count ? row[i] : null;
            }

            try
            {
                transactions.Add(ParseRecord(fields, rowIndex, options));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowIndex}: {ex.Message}");
            }
        }

        return new TransactionFeedResult(transactions, errors);
    }

    private static FraudTransaction ParseJsonElement(JsonElement element, int rowNumber, TransactionIngestionOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Transaction rows must be JSON objects.");
        }

        var fields = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            fields[NormalizeToken(property.Name)] = JsonValueToString(property.Value);
        }

        return ParseRecord(fields, rowNumber, options);
    }

    private static FraudTransaction ParseRecord(IReadOnlyDictionary<string, string?> fields, int rowNumber, TransactionIngestionOptions options)
    {
        var id = Required(fields, rowNumber, "id", "transactionid");
        var merchantId = Required(fields, rowNumber, "merchantid");
        var occurredAt = RequiredDateTimeOffset(fields, rowNumber, "occurredat", "timestamp", "createdat");
        var amount = RequiredDecimal(fields, rowNumber, "amount", "transactionamount");
        var currency = Required(fields, rowNumber, "currency");
        var channel = ParseChannel(Optional(fields, "channel", "transactionchannel"));
        var status = ParseStatus(Optional(fields, "status", "transactionstatus"));
        var cardBrand = Optional(fields, "cardbrand", "brand", "cardtype");
        var cardBin = Optional(fields, "cardbin", "bin", "bankidentificationnumber");
        var cardLast4 = Optional(fields, "cardlast4", "last4", "lastfour");
        var cardNumber = Optional(fields, "cardnumber", "pan", "primaryaccountnumber");

        if (string.IsNullOrWhiteSpace(cardLast4) && !string.IsNullOrWhiteSpace(cardNumber))
        {
            cardLast4 = SensitiveDataMasker.MaskCardNumber(cardNumber);
        }

        var customerId = Optional(fields, "customerid", "customer", "cardholderid");
        var deviceId = Optional(fields, "deviceid", "device");
        var ipAddress = Optional(fields, "ipaddress", "ip", "ipaddr", "remoteip");
        var externalRef = Optional(fields, "externalref", "reference", "ref", "transactionreference");

        if (options.MaskSensitiveData)
        {
            ipAddress = SensitiveDataMasker.MaskIpAddress(ipAddress);
        }

        return new FraudTransaction(
            id,
            merchantId,
            occurredAt,
            new Money(amount, currency),
            channel,
            status,
            cardBrand,
            cardBin,
            cardLast4,
            customerId,
            deviceId,
            ipAddress,
            externalRef);
    }

    private static string? Optional(IReadOnlyDictionary<string, string?> fields, params string[] names)
    {
        foreach (var name in names)
        {
            if (fields.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string Required(IReadOnlyDictionary<string, string?> fields, int rowNumber, params string[] names)
    {
        var value = Optional(fields, names);
        if (value is null)
        {
            throw new FormatException($"Row {rowNumber}: Missing required field '{string.Join("' or '", names)}'.");
        }

        return value;
    }

    private static decimal RequiredDecimal(IReadOnlyDictionary<string, string?> fields, int rowNumber, params string[] names)
    {
        var value = Required(fields, rowNumber, names);
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            throw new FormatException($"Row {rowNumber}: Field '{names[0]}' is not a decimal: '{value}'.");
        }

        return amount;
    }

    private static DateTimeOffset RequiredDateTimeOffset(IReadOnlyDictionary<string, string?> fields, int rowNumber, params string[] names)
    {
        var value = Required(fields, rowNumber, names);
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
        {
            throw new FormatException($"Row {rowNumber}: Field '{names[0]}' is not a timestamp: '{value}'.");
        }

        return timestamp;
    }

    private static TransactionChannel ParseChannel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TransactionChannel.Unknown;
        }

        return NormalizeToken(value) switch
        {
            "cp" or "cardpresent" => TransactionChannel.CardPresent,
            "cnp" or "cardnotpresent" or "online" or "internet" => TransactionChannel.CardNotPresent,
            "moto" or "mailorder" or "telephoneorder" => TransactionChannel.Moto,
            "ecommerce" or "ecom" or "web" => TransactionChannel.Ecommerce,
            "unknown" => TransactionChannel.Unknown,
            _ => ParseEnum<TransactionChannel>(value)
        };
    }

    private static TransactionStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TransactionStatus.Authorized;
        }

        return NormalizeToken(value) switch
        {
            "chargeback" or "chargedback" or "cb" => TransactionStatus.ChargedBack,
            "refund" or "refunded" => TransactionStatus.Refunded,
            "captured" => TransactionStatus.Captured,
            "settled" => TransactionStatus.Settled,
            "declined" => TransactionStatus.Declined,
            "authorized" => TransactionStatus.Authorized,
            _ => ParseEnum<TransactionStatus>(value)
        };
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        var normalized = NormalizeToken(value);
        foreach (var name in Enum.GetNames<T>())
        {
            if (NormalizeToken(name) == normalized)
            {
                return Enum.Parse<T>(name);
            }
        }

        throw new FormatException($"Unknown {typeof(T).Name} value '{value}'.");
    }

    private static bool TryGetJsonProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (NormalizeToken(property.Name) == NormalizeToken(name))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? JsonValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText()
    };

    private static string NormalizeToken(string? value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
