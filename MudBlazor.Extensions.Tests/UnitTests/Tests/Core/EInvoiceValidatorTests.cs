using MudBlazor.Extensions.Core.EInvoice;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

/// <summary>
/// Covers the core rule check: the demo invoices pass, and a document broken on purpose fails on exactly the
/// rule that was broken.
/// </summary>
public class EInvoiceValidatorTests
{
    private static string SampleDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Samples")))
                directory = directory.Parent;

            return Path.Combine(directory!.FullName, "Samples", "MainSample.WebAssembly", "wwwroot", "sample-data");
        }
    }

    private static MudExEInvoice Sample(string name)
        => MudExEInvoiceReader.Read(File.ReadAllBytes(Path.Combine(SampleDirectory, name)));

    private static bool Broke(MudExEInvoiceValidationResult result, string rule)
        => result.Violations.Any(v => v.Rule == rule && v.Severity == MudExEInvoiceSeverity.Error);

    [Fact]
    public void TheSampleInvoicesPassTheCoreRules()
    {
        foreach (var name in new[] { "sample-xrechnung.xml", "sample-factur-x.xml", "sample-zugferd.pdf" })
        {
            var result = MudExEInvoiceValidator.Validate(Sample(name));

            Assert.True(result.IsValid,
                $"{name}: {string.Join(" | ", result.Violations.Select(v => $"{v.Rule} {v.Message}"))}");
        }
    }

    [Fact]
    public void AMissingNumberIsAnError()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.Number = null;

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-02"));
    }

    [Fact]
    public void LineAmountsThatDoNotAddUpAreFound()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.Lines[0].LineTotal += 100m;

        var result = MudExEInvoiceValidator.Validate(invoice);

        // The sum of the lines no longer matches the stated net total...
        Assert.True(Broke(result, "BR-CO-10"));
        // ...and that line's own quantity times price no longer matches either.
        Assert.True(Broke(result, "BR-CO-04"));
    }

    [Fact]
    public void AWrongGrossTotalIsFound()
    {
        var invoice = Sample("sample-factur-x.xml");
        invoice.GrandTotal += 1m;

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-CO-15"));
    }

    [Fact]
    public void AWrongAmountDueIsFound()
    {
        var invoice = Sample("sample-factur-x.xml");
        // Grand total minus the prepayment is what has to be left.
        invoice.DuePayable = invoice.GrandTotal;

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-CO-16"));
    }

    [Fact]
    public void TaxThatDoesNotMatchItsRateIsFound()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.Taxes[0].TaxAmount = 1m;

        var result = MudExEInvoiceValidator.Validate(invoice);

        Assert.True(Broke(result, "BR-CO-17"));
        Assert.True(Broke(result, "BR-CO-14"));
    }

    [Fact]
    public void ARateUsedOnALineMustAppearInTheBreakdown()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.Lines[0].TaxPercent = 7m;

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-CO-18"));
    }

    [Fact]
    public void ABrokenIbanIsFound()
    {
        var invoice = Sample("sample-xrechnung.xml");
        var digits = invoice.Iban.ToCharArray();
        digits[5] = digits[5] == '1' ? '2' : '1';
        invoice.Iban = new string(digits);

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-61"));
    }

    [Fact]
    public void ADueDateBeforeTheInvoiceDateIsFound()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.DueDate = invoice.IssueDate?.AddDays(-1);

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-CO-25"));
    }

    [Fact]
    public void AnXRechnungWithoutALeitwegIdIsFound()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.BuyerReference = null;

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-DE-15"));
    }

    [Fact]
    public void TheGermanRulesOnlyApplyToAnXRechnung()
    {
        // The CII sample is Factur-X, not XRechnung, and its buyer reference is a cost centre.
        var invoice = Sample("sample-factur-x.xml");

        Assert.DoesNotContain(MudExEInvoiceValidator.Validate(invoice).Violations, v => v.Rule.StartsWith("BR-DE"));
    }

    [Fact]
    public void AnInvoiceWithoutLinesIsNotOne()
    {
        var invoice = Sample("sample-xrechnung.xml");
        invoice.Lines.Clear();

        Assert.True(Broke(MudExEInvoiceValidator.Validate(invoice), "BR-16"));
    }

    [Fact]
    public void NothingAtAllIsReported()
    {
        var result = MudExEInvoiceValidator.Validate(null);

        Assert.False(result.IsValid);
        Assert.Single(result.Violations);
    }
}
