using System.Text.Json.Nodes;
using InfobelApiDemos.Formatting;

namespace InfobelApiDemos.Tests.Formatting;

public class FormatAddressTests
{
    [Fact]
    public void FullAddress()
    {
        var record = new JsonObject
        {
            ["addressStreet"] = "Main St",
            ["addressHouseNumber"] = "42",
            ["postCode"] = "10001",
            ["city"] = "New York",
            ["countryName"] = "United States",
        };
        var result = RecordFormatter.FormatAddress(record);
        Assert.Contains("Main St 42", result);
        Assert.Contains("10001 New York", result);
        Assert.Contains("United States", result);
    }

    [Fact]
    public void StreetWithoutNumber()
    {
        var record = new JsonObject
        {
            ["addressStreet"] = "Broadway",
            ["city"] = "LA",
        };
        var result = RecordFormatter.FormatAddress(record);
        Assert.Contains("Broadway", result);
        Assert.Contains("LA", result);
    }

    [Fact]
    public void EmptyRecord()
    {
        Assert.Equal("", RecordFormatter.FormatAddress(new JsonObject()));
    }

    [Fact]
    public void AddressExtra()
    {
        var record = new JsonObject
        {
            ["addressStreet"] = "Elm",
            ["addressExtra"] = "Suite 5",
        };
        var result = RecordFormatter.FormatAddress(record);
        Assert.Contains("Suite 5", result);
    }

    [Fact]
    public void CountryFallback()
    {
        var record = new JsonObject { ["country"] = "US" };
        Assert.Contains("US", RecordFormatter.FormatAddress(record));
    }
}

public class FormatContactFieldsTests
{
    [Fact]
    public void AllFields()
    {
        var record = new JsonObject
        {
            ["phone"] = "123",
            ["website"] = "example.com",
            ["email"] = "a@b.c",
        };
        var fields = RecordFormatter.FormatContactFields(record);
        Assert.Contains("Phone: 123", fields);
        Assert.Contains("Website: example.com", fields);
        Assert.Contains("Email: a@b.c", fields);
    }

    [Fact]
    public void PhoneFallbackToMobile()
    {
        var record = new JsonObject { ["phoneOrMobile"] = "555" };
        var fields = RecordFormatter.FormatContactFields(record);
        Assert.Contains("Phone: 555", fields);
    }

    [Fact]
    public void NoFields()
    {
        Assert.Empty(RecordFormatter.FormatContactFields(new JsonObject()));
    }
}

public class PrintResultsTests
{
    [Fact]
    public void FullRecord()
    {
        var records = new JsonArray
        {
            new JsonObject
            {
                ["companyName"] = "Nvidia",
                ["addressStreet"] = "Main",
                ["addressHouseNumber"] = "1",
                ["postCode"] = "95050",
                ["city"] = "Santa Clara",
                ["countryName"] = "US",
                ["uniqueID"] = "U1",
                ["phone"] = "555-0100",
                ["website"] = "nvidia.com",
                ["email"] = "info@nvidia.com",
                ["latitude"] = 37.35,
                ["longitude"] = -121.95,
                ["internationalLabel01"] = "Semiconductors",
            }
        };

        var writer = new StringWriter();
        RecordFormatter.PrintResults(records, writer);
        var output = writer.ToString();

        Assert.Contains("Nvidia", output);
        Assert.Contains("UniqueID: U1", output);
        Assert.Contains("Phone: 555-0100", output);
        Assert.Contains("Website: nvidia.com", output);
        Assert.Contains("Email: info@nvidia.com", output);
        Assert.Contains("37.35", output);
        Assert.Contains("OpenStreetMap", output);
        Assert.Contains("Semiconductors", output);
    }

    [Fact]
    public void MinimalRecord()
    {
        var records = new JsonArray { new JsonObject() };
        var writer = new StringWriter();
        RecordFormatter.PrintResults(records, writer);
        var output = writer.ToString();

        Assert.Contains("<Unknown>", output);
        Assert.Contains("<No address provided>", output);
    }
}
