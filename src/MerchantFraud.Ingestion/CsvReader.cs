using System.Text;

namespace MerchantFraud.Ingestion;

internal static class CsvReader
{
    public static IReadOnlyList<IReadOnlyList<string>> Parse(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                {
                    rows.Add(row);
                }
                row = new List<string>();
            }
            else if (c != '\r')
            {
                field.Append(c);
            }
        }

        row.Add(field.ToString());
        if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
        {
            rows.Add(row);
        }

        return rows;
    }
}
