namespace MudBlazor.Extensions.Core.EInvoice;

/// <summary>
/// An electronic invoice, as far as a reader needs it: who, what, how much, until when.
/// </summary>
/// <remarks>
/// EN 16931 knows hundreds of fields. This is the subset that belongs on a rendered invoice, filled from
/// either of the two syntaxes the standard allows - UN/CEFACT CII and OASIS UBL - so the rest of the library
/// never has to know which one a file used.
/// </remarks>
public class MudExEInvoice
{
    /// <summary>Which syntax the data came from.</summary>
    public MudExEInvoiceSyntax Syntax { get; set; }

    /// <summary>The specification the document claims to follow, like XRechnung or an EN 16931 profile.</summary>
    public string Profile { get; set; }

    /// <summary>Invoice number (BT-1).</summary>
    public string Number { get; set; }

    /// <summary>Invoice date (BT-2).</summary>
    public DateTime? IssueDate { get; set; }

    /// <summary>Due date (BT-9).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Document type code (BT-3), 380 for an invoice, 381 for a credit note.</summary>
    public string TypeCode { get; set; }

    /// <summary>Currency of every amount (BT-5).</summary>
    public string Currency { get; set; }

    /// <summary>Buyer reference (BT-10) - the Leitweg-ID for German public authorities.</summary>
    public string BuyerReference { get; set; }

    /// <summary>Order reference (BT-13).</summary>
    public string OrderReference { get; set; }

    /// <summary>Free text of the document (BT-22).</summary>
    public string Note { get; set; }

    /// <summary>Who issues the invoice.</summary>
    public MudExEInvoiceParty Seller { get; set; } = new();

    /// <summary>Who has to pay it.</summary>
    public MudExEInvoiceParty Buyer { get; set; } = new();

    /// <summary>Payment terms as text (BT-20).</summary>
    public string PaymentTerms { get; set; }

    /// <summary>Account the money goes to (BT-84).</summary>
    public string Iban { get; set; }

    /// <summary>Bank identifier (BT-86).</summary>
    public string Bic { get; set; }

    /// <summary>Sum of all line amounts (BT-106).</summary>
    public decimal? LineTotal { get; set; }

    /// <summary>Amount without tax (BT-109).</summary>
    public decimal? TaxBasisTotal { get; set; }

    /// <summary>Tax (BT-110).</summary>
    public decimal? TaxTotal { get; set; }

    /// <summary>Amount including tax (BT-112).</summary>
    public decimal? GrandTotal { get; set; }

    /// <summary>Already paid (BT-113).</summary>
    public decimal? PrepaidAmount { get; set; }

    /// <summary>What is left to pay (BT-115).</summary>
    public decimal? DuePayable { get; set; }

    /// <summary>The invoiced items.</summary>
    public List<MudExEInvoiceLine> Lines { get; set; } = new();

    /// <summary>Tax per rate (BG-23).</summary>
    public List<MudExEInvoiceTax> Taxes { get; set; } = new();

    /// <summary>The document the invoice was read from, when it came inside a pdf.</summary>
    public string EmbeddedFileName { get; set; }

    /// <summary>The xml itself, for the raw view.</summary>
    public string Xml { get; set; }

    /// <summary>A credit note rather than an invoice.</summary>
    public bool IsCreditNote => TypeCode == "381";
}

/// <summary>The syntax an invoice was written in.</summary>
public enum MudExEInvoiceSyntax
{
    /// <summary>UN/CEFACT Cross Industry Invoice, what ZUGFeRD and Factur-X carry.</summary>
    Cii,

    /// <summary>OASIS UBL Invoice or CreditNote, what XRechnung uses by default.</summary>
    Ubl
}

/// <summary>A party of an invoice.</summary>
public class MudExEInvoiceParty
{
    /// <summary>Registered name.</summary>
    public string Name { get; set; }

    /// <summary>Vat identifier.</summary>
    public string VatId { get; set; }

    /// <summary>Street and number.</summary>
    public string Street { get; set; }

    /// <summary>Postal code.</summary>
    public string PostalCode { get; set; }

    /// <summary>City.</summary>
    public string City { get; set; }

    /// <summary>Country code.</summary>
    public string Country { get; set; }

    /// <summary>Contact mail address.</summary>
    public string Email { get; set; }

    /// <summary>Everything of the address on one line.</summary>
    public string AddressLine => string.Join(", ", new[] { Street, string.Join(" ", new[] { PostalCode, City }.Where(s => !string.IsNullOrWhiteSpace(s))), Country }
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>True when nothing at all was found.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(AddressLine);
}

/// <summary>One invoiced item.</summary>
public class MudExEInvoiceLine
{
    /// <summary>Position number.</summary>
    public string Id { get; set; }

    /// <summary>What was sold.</summary>
    public string Name { get; set; }

    /// <summary>Further description.</summary>
    public string Description { get; set; }

    /// <summary>How much of it.</summary>
    public decimal? Quantity { get; set; }

    /// <summary>Unit of the quantity.</summary>
    public string Unit { get; set; }

    /// <summary>Price of one unit without tax.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Tax rate in percent.</summary>
    public decimal? TaxPercent { get; set; }

    /// <summary>Line amount without tax.</summary>
    public decimal? LineTotal { get; set; }
}

/// <summary>Tax for one rate.</summary>
public class MudExEInvoiceTax
{
    /// <summary>Category code, S for the standard rate.</summary>
    public string Category { get; set; }

    /// <summary>Rate in percent.</summary>
    public decimal? Percent { get; set; }

    /// <summary>Amount the rate applies to.</summary>
    public decimal? BasisAmount { get; set; }

    /// <summary>The tax itself.</summary>
    public decimal? TaxAmount { get; set; }
}
