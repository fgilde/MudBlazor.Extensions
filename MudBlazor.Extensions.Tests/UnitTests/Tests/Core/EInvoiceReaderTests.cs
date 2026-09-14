using MudBlazor.Extensions.Core.EInvoice;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

/// <summary>
/// Covers reading an electronic invoice out of both syntaxes the standard allows, and out of the pdf a
/// ZUGFeRD file is.
/// </summary>
public class EInvoiceReaderTests
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

    private static byte[] Sample(string name) => File.ReadAllBytes(Path.Combine(SampleDirectory, name));

    [Fact]
    public void AnXRechnungIsReadFromUbl()
    {
        var invoice = MudExEInvoiceReader.Read(Sample("sample-xrechnung.xml"));

        Assert.NotNull(invoice);
        Assert.Equal(MudExEInvoiceSyntax.Ubl, invoice.Syntax);
        Assert.Equal("RE-2026-0815", invoice.Number);
        Assert.Equal(new DateTime(2026, 2, 3), invoice.IssueDate);
        Assert.Equal(new DateTime(2026, 3, 5), invoice.DueDate);
        Assert.Equal("EUR", invoice.Currency);
        Assert.Equal("04011000-12345-67", invoice.BuyerReference);
        Assert.Contains("xrechnung", invoice.Profile);
    }

    [Fact]
    public void TheUblPartiesAndTotalsAreComplete()
    {
        var invoice = MudExEInvoiceReader.Read(Sample("sample-xrechnung.xml"));

        Assert.Equal("Nordlicht Software GmbH", invoice.Seller.Name);
        Assert.Equal("DE812345678", invoice.Seller.VatId);
        Assert.Contains("Hamburg", invoice.Seller.AddressLine);
        Assert.Equal("Stadtwerke Musterstadt AoeR", invoice.Buyer.Name);

        Assert.Equal(2600.00m, invoice.LineTotal);
        Assert.Equal(494.00m, invoice.TaxTotal);
        Assert.Equal(3094.00m, invoice.GrandTotal);
        Assert.Equal(3094.00m, invoice.DuePayable);
        Assert.Equal("DE02200505501015871393", invoice.Iban);
    }

    [Fact]
    public void TheUblLinesAreRead()
    {
        var invoice = MudExEInvoiceReader.Read(Sample("sample-xrechnung.xml"));

        Assert.Equal(2, invoice.Lines.Count);
        var first = invoice.Lines[0];
        Assert.Equal("Entwicklung Rechnungsmodul", first.Name);
        Assert.Equal(16m, first.Quantity);
        Assert.Equal("HUR", first.Unit);
        Assert.Equal(115.00m, first.UnitPrice);
        Assert.Equal(19.00m, first.TaxPercent);
        Assert.Equal(1840.00m, first.LineTotal);
    }

    [Fact]
    public void AFacturXDocumentIsReadFromCii()
    {
        var invoice = MudExEInvoiceReader.Read(Sample("sample-factur-x.xml"));

        Assert.NotNull(invoice);
        Assert.Equal(MudExEInvoiceSyntax.Cii, invoice.Syntax);
        Assert.Equal("ZF-2026-0042", invoice.Number);
        // CII writes dates as 20260210.
        Assert.Equal(new DateTime(2026, 2, 10), invoice.IssueDate);
        Assert.Equal(new DateTime(2026, 3, 12), invoice.DueDate);
        Assert.Equal("Alpenblick IT Services GmbH", invoice.Seller.Name);
        Assert.Equal("Weber Maschinenbau KG", invoice.Buyer.Name);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal(1950.00m, invoice.TaxBasisTotal);
        Assert.Equal(370.50m, invoice.TaxTotal);
        Assert.Equal(2320.50m, invoice.GrandTotal);
        // What is left after the prepayment - the number that belongs on top of a reader.
        Assert.Equal(2000.00m, invoice.DuePayable);
    }

    [Fact]
    public void TheTaxBreakdownIsRead()
    {
        var cii = MudExEInvoiceReader.Read(Sample("sample-factur-x.xml"));
        var ubl = MudExEInvoiceReader.Read(Sample("sample-xrechnung.xml"));

        var ciiTax = Assert.Single(cii.Taxes);
        Assert.Equal("S", ciiTax.Category);
        Assert.Equal(19.00m, ciiTax.Percent);
        Assert.Equal(370.50m, ciiTax.TaxAmount);

        var ublTax = Assert.Single(ubl.Taxes);
        Assert.Equal(19.00m, ublTax.Percent);
        Assert.Equal(2600.00m, ublTax.BasisAmount);
    }

    [Fact]
    public void TheInvoiceInsideAZugferdPdfIsFound()
    {
        var bytes = Sample("sample-zugferd.pdf");

        Assert.True(MudExPdfAttachments.IsPdf(bytes));

        var invoice = MudExEInvoiceReader.Read(bytes);

        Assert.NotNull(invoice);
        Assert.Equal(MudExEInvoiceSyntax.Cii, invoice.Syntax);
        Assert.Equal("ZF-2026-0042", invoice.Number);
        Assert.Equal("factur-x.xml", invoice.EmbeddedFileName);
        Assert.Equal(2000.00m, invoice.DuePayable);
    }

    [Fact]
    public void APdfWithoutAnInvoiceIsNotOne()
    {
        // The plain sample pdf of the demo carries no attachment.
        var invoice = MudExEInvoiceReader.Read(Sample("sample.pdf"));

        Assert.Null(invoice);
    }

    [Fact]
    public void SomethingElseEntirelyIsRefused()
    {
        Assert.Null(MudExEInvoiceReader.Read(System.Text.Encoding.UTF8.GetBytes("<root><child/></root>")));
        Assert.Null(MudExEInvoiceReader.Read(System.Text.Encoding.UTF8.GetBytes("not xml at all")));
        Assert.Null(MudExEInvoiceReader.Read(Array.Empty<byte>()));
        Assert.Null(MudExEInvoiceReader.Read(null));
    }

    [Fact]
    public void OnlyXmlAndPdfAreWorthReading()
    {
        Assert.True(MudExEInvoiceReader.LooksLikeEInvoice("invoice.xml", null));
        Assert.True(MudExEInvoiceReader.LooksLikeEInvoice("invoice.pdf", null));
        Assert.True(MudExEInvoiceReader.LooksLikeEInvoice("no-extension", "application/xml"));
        Assert.False(MudExEInvoiceReader.LooksLikeEInvoice("holiday.jpg", "image/jpeg"));
    }
}
