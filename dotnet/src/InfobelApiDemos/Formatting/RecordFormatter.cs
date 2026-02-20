namespace InfobelApiDemos.Formatting;

using System.Text.Json.Nodes;

public static class RecordFormatter
{
    /// <summary>
    /// Format address fields from a record into a comma-separated string.
    /// </summary>
    public static string FormatAddress(JsonObject record)
    {
        var parts = new List<string>();

        var street = GetString(record, "addressStreet");
        var number = GetString(record, "addressHouseNumber");
        if (street is not null && number is not null)
            parts.Add($"{street} {number}");
        else if (street is not null)
            parts.Add(street);

        var extra = GetString(record, "addressExtra");
        if (extra is not null)
            parts.Add(extra);

        var postCode = GetString(record, "postCode");
        var city = GetString(record, "city");
        var localityParts = new[] { postCode, city }.Where(p => p is not null);
        var locality = string.Join(" ", localityParts);
        if (!string.IsNullOrEmpty(locality))
            parts.Add(locality);

        var country = GetString(record, "countryName") ?? GetString(record, "country");
        if (country is not null)
            parts.Add(country);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Extract formatted contact fields (Phone, Website, Email).
    /// </summary>
    public static List<string> FormatContactFields(JsonObject record)
    {
        var fields = new List<string>();

        var phone = GetString(record, "phone") ?? GetString(record, "phoneOrMobile");
        if (phone is not null) fields.Add($"Phone: {phone}");

        var website = GetString(record, "website");
        if (website is not null) fields.Add($"Website: {website}");

        var email = GetString(record, "email");
        if (email is not null) fields.Add($"Email: {email}");

        return fields;
    }

    /// <summary>
    /// Print a list of records to the given output in the standard demo format.
    /// </summary>
    public static void PrintResults(JsonArray records, TextWriter? output = null)
    {
        var writer = output ?? Console.Out;

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i]?.AsObject();
            if (record is null) continue;

            var index = i + 1;
            var company = GetString(record, "companyName")
                ?? GetString(record, "businessName")
                ?? "<Unknown>";
            var address = FormatAddress(record);
            var addressDisplay = string.IsNullOrEmpty(address) ? "<No address provided>" : address;

            writer.WriteLine($"Result {index}:");

            var uniqueId = GetString(record, "uniqueID");
            if (uniqueId is not null)
                writer.WriteLine($"  UniqueID: {uniqueId}");

            writer.WriteLine($"  Company: {company}");
            writer.WriteLine($"  Address: {addressDisplay}");

            foreach (var field in FormatContactFields(record))
                writer.WriteLine($"  {field}");

            var latitude = GetCoord(record, "latitude");
            var longitude = GetCoord(record, "longitude");
            if (latitude is not null && longitude is not null)
            {
                writer.WriteLine($"  Location: {latitude}, {longitude}");
                writer.WriteLine($"  OpenStreetMap: https://www.openstreetmap.org/?mlat={latitude}&mlon={longitude}");
            }

            var activity = GetString(record, "internationalLabel01")
                ?? GetString(record, "altInternationalLabel01");
            if (activity is not null)
                writer.WriteLine($"  Activity: {activity}");

            writer.WriteLine();
        }
    }

    private static string? GetString(JsonObject record, string key)
    {
        if (record.TryGetPropertyValue(key, out var node) && node is JsonValue value)
        {
            var str = value.ToString();
            return string.IsNullOrEmpty(str) ? null : str;
        }
        return null;
    }

    private static string? GetCoord(JsonObject record, string key)
    {
        if (!record.TryGetPropertyValue(key, out var node) || node is null)
            return null;

        if (node is JsonValue value)
        {
            // Could be a number or a string
            if (value.TryGetValue<double>(out var d))
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var str = value.ToString();
            return string.IsNullOrEmpty(str) ? null : str;
        }
        return null;
    }
}
