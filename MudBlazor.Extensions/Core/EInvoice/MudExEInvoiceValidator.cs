using System.Globalization;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Core.EInvoice;

/// <summary>
/// Checks an electronic invoice against the core rules of EN 16931: what has to be there, and whether the
/// numbers add up.
/// </summary>
/// <remarks>
/// This is not a conformance check and does not pretend to be one. The full rule set is published as
/// schematron, which needs an XSLT 2.0 engine, and a formal test report is what the KoSIT tool is for. What
/// is implemented here are the rules that can be decided from the document alone - missing content and
/// arithmetic - which is where real invoices actually fail.
/// </remarks>
public static class MudExEInvoiceValidator
{
    /// <summary>Amounts are compared to the cent; anything below that is rounding, not an error.</summary>
    private const decimal Tolerance = 0.01m;

    private static readonly Regex VatIdPattern = new(@"^[A-Z]{2}[A-Za-z0-9\+\*\.]{2,20}$", RegexOptions.Compiled);
    private static readonly Regex IbanPattern = new(@"^[A-Z]{2}[0-9]{2}[A-Za-z0-9]{10,30}$", RegexOptions.Compiled);
    private static readonly Regex LeitwegPattern = new(@"^[0-9]{2,12}(-[A-Za-z0-9]{1,30})?-[0-9]{2}$", RegexOptions.Compiled);

    /// <summary>Checks an invoice and returns what is wrong with it.</summary>
    public static MudExEInvoiceValidationResult Validate(MudExEInvoice invoice)
    {
        var result = new MudExEInvoiceValidationResult();
        if (invoice == null)
        {
            result.Add("BR-01", "No invoice could be read from the file.", MudExEInvoiceSeverity.Error);
            return result;
        }

        ValidateContent(invoice, result);
        ValidateTotals(invoice, result);
        ValidateLines(invoice, result);
        ValidateTaxes(invoice, result);
        ValidatePayment(invoice, result);
        ValidateGermanRules(invoice, result);

        return result;
    }

    private static void ValidateContent(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        Require(result, "BR-01", "An invoice needs a specification identifier.", invoice.Profile, MudExEInvoiceSeverity.Warning);
        Require(result, "BR-02", "An invoice needs an invoice number.", invoice.Number);
        Require(result, "BR-03", "An invoice needs an issue date.", invoice.IssueDate?.ToString(CultureInfo.InvariantCulture));
        Require(result, "BR-04", "An invoice needs a type code.", invoice.TypeCode);
        Require(result, "BR-05", "An invoice needs a currency.", invoice.Currency);
        Require(result, "BR-06", "An invoice needs the seller name.", invoice.Seller?.Name);
        Require(result, "BR-07", "An invoice needs the buyer name.", invoice.Buyer?.Name);

        if (!invoice.Lines.Any())
            result.Add("BR-16", "An invoice needs at least one line item.", MudExEInvoiceSeverity.Error);

        if (invoice.TypeCode is { Length: > 0 } and not ("380" or "381" or "384" or "389" or "875" or "876" or "877"))
            result.Add("BR-CL-01", $"The document type code {invoice.TypeCode} is not one of the invoice types.", MudExEInvoiceSeverity.Warning);

        if (invoice.Currency is { Length: > 0 } currency && currency.Length != 3)
            result.Add("BR-CL-04", $"The currency {currency} is not a three letter code.", MudExEInvoiceSeverity.Error);

        foreach (var (party, rule, label) in new[]
                 {
                     (invoice.Seller, "BR-08", "seller"),
                     (invoice.Buyer, "BR-10", "buyer")
                 })
        {
            if (party != null && string.IsNullOrWhiteSpace(party.AddressLine))
                result.Add(rule, $"The {label} has no postal address.", MudExEInvoiceSeverity.Warning);

            if (party?.VatId is { Length: > 0 } vatId && !VatIdPattern.IsMatch(vatId))
                result.Add("BR-CO-09", $"The {label} vat identifier {vatId} does not start with a country code.", MudExEInvoiceSeverity.Warning);
        }
    }

    private static void ValidateTotals(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        Require(result, "BR-12", "An invoice needs a sum of the line amounts.", invoice.LineTotal?.ToString(CultureInfo.InvariantCulture));
        Require(result, "BR-13", "An invoice needs an amount without tax.", invoice.TaxBasisTotal?.ToString(CultureInfo.InvariantCulture));
        Require(result, "BR-14", "An invoice needs an amount including tax.", invoice.GrandTotal?.ToString(CultureInfo.InvariantCulture));
        Require(result, "BR-15", "An invoice needs the amount due for payment.", invoice.DuePayable?.ToString(CultureInfo.InvariantCulture));

        if (invoice.Lines.Any() && invoice.Lines.All(l => l.LineTotal != null) && invoice.LineTotal != null)
        {
            var sum = invoice.Lines.Sum(l => l.LineTotal!.Value);
            Compare(result, "BR-CO-10", "The sum of the line amounts", sum, invoice.LineTotal.Value, invoice.Currency);
        }

        if (invoice.TaxBasisTotal != null && invoice.TaxTotal != null && invoice.GrandTotal != null)
            Compare(result, "BR-CO-15", "The amount including tax", invoice.TaxBasisTotal.Value + invoice.TaxTotal.Value, invoice.GrandTotal.Value, invoice.Currency);

        if (invoice.GrandTotal != null && invoice.DuePayable != null)
        {
            var expected = invoice.GrandTotal.Value - (invoice.PrepaidAmount ?? 0m);
            Compare(result, "BR-CO-16", "The amount due for payment", expected, invoice.DuePayable.Value, invoice.Currency);
        }

        if (invoice.PrepaidAmount is { } prepaid && invoice.GrandTotal is { } grand && prepaid > grand + Tolerance)
            result.Add("BR-CO-16", "More was prepaid than the invoice totals.", MudExEInvoiceSeverity.Error);
    }

    private static void ValidateLines(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        foreach (var line in invoice.Lines)
        {
            var where = string.IsNullOrWhiteSpace(line.Id) ? "A line" : $"Line {line.Id}";

            if (string.IsNullOrWhiteSpace(line.Name))
                result.Add("BR-25", $"{where} has no item name.", MudExEInvoiceSeverity.Error);

            if (line.Quantity == null)
                result.Add("BR-22", $"{where} has no quantity.", MudExEInvoiceSeverity.Error);

            if (line.LineTotal == null)
                result.Add("BR-24", $"{where} has no line amount.", MudExEInvoiceSeverity.Error);

            if (line.Quantity is { } quantity && line.UnitPrice is { } price && line.LineTotal is { } total)
                Compare(result, "BR-CO-04", $"{where}: quantity times unit price", quantity * price, total, invoice.Currency);
        }
    }

    private static void ValidateTaxes(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        if (!invoice.Taxes.Any())
        {
            if (invoice.TaxTotal is > 0)
                result.Add("BR-45", "Tax is charged but the invoice has no tax breakdown.", MudExEInvoiceSeverity.Error);
            return;
        }

        foreach (var tax in invoice.Taxes)
        {
            if (tax.BasisAmount is { } basis && tax.Percent is { } percent && tax.TaxAmount is { } amount)
            {
                var expected = Math.Round(basis * percent / 100m, 2, MidpointRounding.AwayFromZero);
                Compare(result, "BR-CO-17", $"Tax at {percent:0.##} %", expected, amount, invoice.Currency);
            }

            if (string.IsNullOrWhiteSpace(tax.Category))
                result.Add("BR-47", "A tax breakdown entry has no category code.", MudExEInvoiceSeverity.Warning);
        }

        if (invoice.TaxTotal is { } taxTotal && invoice.Taxes.All(t => t.TaxAmount != null))
            Compare(result, "BR-CO-14", "The total tax", invoice.Taxes.Sum(t => t.TaxAmount!.Value), taxTotal, invoice.Currency);

        // Every rate that is charged on a line has to appear in the breakdown.
        foreach (var percent in invoice.Lines.Where(l => l.TaxPercent != null).Select(l => l.TaxPercent!.Value).Distinct())
        {
            if (invoice.Taxes.All(t => t.Percent != percent))
                result.Add("BR-CO-18", $"Lines are taxed at {percent:0.##} % but that rate is missing from the tax breakdown.", MudExEInvoiceSeverity.Error);
        }
    }

    private static void ValidatePayment(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        if (invoice.DueDate is { } due && invoice.IssueDate is { } issued && due < issued)
            result.Add("BR-CO-25", "The due date is before the invoice date.", MudExEInvoiceSeverity.Error);

        if (invoice.DueDate == null && string.IsNullOrWhiteSpace(invoice.PaymentTerms) && invoice.DuePayable is > 0)
            result.Add("BR-CO-25", "An amount is due but the invoice states neither a due date nor payment terms.", MudExEInvoiceSeverity.Warning);

        if (invoice.Iban is not { Length: > 0 } iban)
            return;

        var normalized = iban.Replace(" ", string.Empty).ToUpperInvariant();
        if (!IbanPattern.IsMatch(normalized) || !HasValidIbanChecksum(normalized))
            result.Add("BR-61", $"The account identifier {iban} is not a valid IBAN.", MudExEInvoiceSeverity.Error);
    }

    private static void ValidateGermanRules(MudExEInvoice invoice, MudExEInvoiceValidationResult result)
    {
        if (invoice.Profile?.Contains("xrechnung", StringComparison.OrdinalIgnoreCase) != true)
            return;

        if (string.IsNullOrWhiteSpace(invoice.BuyerReference))
        {
            result.Add("BR-DE-15", "An XRechnung needs the buyer reference, the Leitweg-ID.", MudExEInvoiceSeverity.Error);
            return;
        }

        if (!LeitwegPattern.IsMatch(invoice.BuyerReference.Trim()))
            result.Add("BR-DE-15", $"The buyer reference {invoice.BuyerReference} is not shaped like a Leitweg-ID.", MudExEInvoiceSeverity.Warning);

        if (string.IsNullOrWhiteSpace(invoice.Seller?.Email))
            result.Add("BR-DE-06", "An XRechnung needs a contact mail address of the seller.", MudExEInvoiceSeverity.Warning);
    }

    /// <summary>The mod 97 check of ISO 13616 - the one thing about an IBAN that can be decided offline.</summary>
    private static bool HasValidIbanChecksum(string iban)
    {
        var rearranged = iban.Substring(4) + iban.Substring(0, 4);
        var remainder = 0;

        foreach (var character in rearranged)
        {
            var value = char.IsDigit(character) ? character - '0' : char.IsLetter(character) ? character - 'A' + 10 : -1;
            if (value < 0)
                return false;

            remainder = value > 9 ? (remainder * 100 + value) % 97 : (remainder * 10 + value) % 97;
        }

        return remainder == 1;
    }

    private static void Require(MudExEInvoiceValidationResult result, string rule, string message, string value,
        MudExEInvoiceSeverity severity = MudExEInvoiceSeverity.Error)
    {
        if (string.IsNullOrWhiteSpace(value))
            result.Add(rule, message, severity);
    }

    private static void Compare(MudExEInvoiceValidationResult result, string rule, string what, decimal expected,
        decimal actual, string currency)
    {
        if (Math.Abs(expected - actual) <= Tolerance)
            return;

        result.Add(rule, $"{what} should be {expected:N2} {currency} but the invoice says {actual:N2} {currency}.",
            MudExEInvoiceSeverity.Error);
    }
}
