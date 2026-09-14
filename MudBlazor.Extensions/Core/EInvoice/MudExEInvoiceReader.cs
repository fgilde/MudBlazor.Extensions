using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Core.EInvoice;

/// <summary>
/// Reads an electronic invoice out of xml, or out of the xml a pdf carries inside it.
/// </summary>
/// <remarks>
/// Elements are matched by local name. The standard allows several namespace versions per syntax and every
/// profile adds its own, so binding to one of them would reject documents that are perfectly valid.
/// </remarks>
public static class MudExEInvoiceReader
{
    /// <summary>Reads an invoice from a file's bytes, whatever of the two syntaxes - or a pdf - it is.</summary>
    public static MudExEInvoice Read(byte[] bytes)
    {
        if (bytes is not { Length: > 0 })
            return null;

        if (MudExPdfAttachments.IsPdf(bytes))
        {
            foreach (var (name, content) in MudExPdfAttachments.ReadXmlAttachments(bytes))
            {
                var fromPdf = ReadXml(content);
                if (fromPdf == null)
                    continue;

                fromPdf.EmbeddedFileName = name;
                return fromPdf;
            }

            return null;
        }

        return ReadXml(Encoding.UTF8.GetString(bytes));
    }

    /// <summary>Reads an invoice from xml, or returns null when the xml is not one.</summary>
    public static MudExEInvoice ReadXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception)
        {
            return null;
        }

        var invoice = document.Root?.Name.LocalName switch
        {
            "CrossIndustryInvoice" => ReadCii(document),
            "Invoice" or "CreditNote" => ReadUbl(document),
            _ => null
        };

        if (invoice != null)
            invoice.Xml = xml;

        return invoice;
    }

    /// <summary>Whether a file of that name and content type is worth handing to <see cref="Read"/>.</summary>
    public static bool LooksLikeEInvoice(string fileName, string contentType)
    {
        var extension = System.IO.Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension is ".xml" or ".pdf"
               || contentType?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true
               || contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static MudExEInvoice ReadCii(XDocument document)
    {
        var root = document.Root;
        var invoice = new MudExEInvoice
        {
            Syntax = MudExEInvoiceSyntax.Cii,
            Profile = Value(root, "ExchangedDocumentContext", "GuidelineSpecifiedDocumentContextParameter", "ID"),
            Number = Value(root, "ExchangedDocument", "ID"),
            TypeCode = Value(root, "ExchangedDocument", "TypeCode"),
            IssueDate = Date(Value(root, "ExchangedDocument", "IssueDateTime", "DateTimeString")),
            Note = Value(root, "ExchangedDocument", "IncludedNote", "Content")
        };

        var transaction = Element(root, "SupplyChainTradeTransaction");
        var agreement = Element(transaction, "ApplicableHeaderTradeAgreement");
        var settlement = Element(transaction, "ApplicableHeaderTradeSettlement");

        invoice.BuyerReference = Value(agreement, "BuyerReference");
        invoice.OrderReference = Value(agreement, "BuyerOrderReferencedDocument", "IssuerAssignedID");
        invoice.Seller = ReadCiiParty(Element(agreement, "SellerTradeParty"));
        invoice.Buyer = ReadCiiParty(Element(agreement, "BuyerTradeParty"));

        invoice.Currency = Value(settlement, "InvoiceCurrencyCode");
        invoice.PaymentTerms = Value(settlement, "SpecifiedTradePaymentTerms", "Description");
        invoice.DueDate = Date(Value(settlement, "SpecifiedTradePaymentTerms", "DueDateDateTime", "DateTimeString"));
        invoice.Iban = Value(settlement, "SpecifiedTradeSettlementPaymentMeans", "PayeePartyCreditorFinancialAccount", "IBANID");
        invoice.Bic = Value(settlement, "SpecifiedTradeSettlementPaymentMeans", "PayeeSpecifiedCreditorFinancialInstitution", "BICID");

        var summation = Element(settlement, "SpecifiedTradeSettlementHeaderMonetarySummation");
        invoice.LineTotal = Amount(Value(summation, "LineTotalAmount"));
        invoice.TaxBasisTotal = Amount(Value(summation, "TaxBasisTotalAmount"));
        invoice.TaxTotal = Amount(Value(summation, "TaxTotalAmount"));
        invoice.GrandTotal = Amount(Value(summation, "GrandTotalAmount"));
        invoice.PrepaidAmount = Amount(Value(summation, "TotalPrepaidAmount"));
        invoice.DuePayable = Amount(Value(summation, "DuePayableAmount"));

        foreach (var tax in Elements(settlement, "ApplicableTradeTax"))
        {
            invoice.Taxes.Add(new MudExEInvoiceTax
            {
                Category = Value(tax, "CategoryCode"),
                Percent = Amount(Value(tax, "RateApplicablePercent")),
                BasisAmount = Amount(Value(tax, "BasisAmount")),
                TaxAmount = Amount(Value(tax, "CalculatedAmount"))
            });
        }

        foreach (var line in Elements(transaction, "IncludedSupplyChainTradeLineItem"))
        {
            var lineAgreement = Element(line, "SpecifiedLineTradeAgreement");
            var lineSettlement = Element(line, "SpecifiedLineTradeSettlement");
            var quantity = Element(line, "SpecifiedLineTradeDelivery")?.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "BilledQuantity");

            invoice.Lines.Add(new MudExEInvoiceLine
            {
                Id = Value(line, "AssociatedDocumentLineDocument", "LineID"),
                Name = Value(line, "SpecifiedTradeProduct", "Name"),
                Description = Value(line, "SpecifiedTradeProduct", "Description"),
                Quantity = Amount(quantity?.Value),
                Unit = quantity?.Attribute("unitCode")?.Value,
                UnitPrice = Amount(Value(lineAgreement, "NetPriceProductTradePrice", "ChargeAmount")),
                TaxPercent = Amount(Value(lineSettlement, "ApplicableTradeTax", "RateApplicablePercent")),
                LineTotal = Amount(Value(lineSettlement, "SpecifiedTradeSettlementLineMonetarySummation", "LineTotalAmount"))
            });
        }

        return invoice;
    }

    private static MudExEInvoiceParty ReadCiiParty(XElement party)
    {
        if (party == null)
            return new MudExEInvoiceParty();

        var address = Element(party, "PostalTradeAddress");
        return new MudExEInvoiceParty
        {
            Name = Value(party, "Name"),
            VatId = Elements(party, "SpecifiedTaxRegistration")
                .Select(r => Value(r, "ID"))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)),
            Street = Value(address, "LineOne"),
            PostalCode = Value(address, "PostcodeCode"),
            City = Value(address, "CityName"),
            Country = Value(address, "CountryID"),
            Email = Value(party, "DefinedTradeContact", "EmailURIUniversalCommunication", "URIID")
        };
    }

    private static MudExEInvoice ReadUbl(XDocument document)
    {
        var root = document.Root;
        var invoice = new MudExEInvoice
        {
            Syntax = MudExEInvoiceSyntax.Ubl,
            Profile = Value(root, "CustomizationID"),
            Number = Value(root, "ID"),
            TypeCode = Value(root, "InvoiceTypeCode") ?? Value(root, "CreditNoteTypeCode"),
            IssueDate = Date(Value(root, "IssueDate")),
            DueDate = Date(Value(root, "DueDate")),
            Currency = Value(root, "DocumentCurrencyCode"),
            BuyerReference = Value(root, "BuyerReference"),
            OrderReference = Value(root, "OrderReference", "ID"),
            Note = Value(root, "Note"),
            Seller = ReadUblParty(Element(Element(root, "AccountingSupplierParty"), "Party")),
            Buyer = ReadUblParty(Element(Element(root, "AccountingCustomerParty"), "Party")),
            PaymentTerms = Value(root, "PaymentTerms", "Note"),
            Iban = Value(root, "PaymentMeans", "PayeeFinancialAccount", "ID"),
            Bic = Value(root, "PaymentMeans", "PayeeFinancialAccount", "FinancialInstitutionBranch", "ID")
        };

        var totals = Element(root, "LegalMonetaryTotal");
        invoice.LineTotal = Amount(Value(totals, "LineExtensionAmount"));
        invoice.TaxBasisTotal = Amount(Value(totals, "TaxExclusiveAmount"));
        invoice.GrandTotal = Amount(Value(totals, "TaxInclusiveAmount"));
        invoice.PrepaidAmount = Amount(Value(totals, "PrepaidAmount"));
        invoice.DuePayable = Amount(Value(totals, "PayableAmount"));

        var taxTotal = Element(root, "TaxTotal");
        invoice.TaxTotal = Amount(Value(taxTotal, "TaxAmount"));
        foreach (var subtotal in Elements(taxTotal, "TaxSubtotal"))
        {
            invoice.Taxes.Add(new MudExEInvoiceTax
            {
                Category = Value(subtotal, "TaxCategory", "ID"),
                Percent = Amount(Value(subtotal, "TaxCategory", "Percent")),
                BasisAmount = Amount(Value(subtotal, "TaxableAmount")),
                TaxAmount = Amount(Value(subtotal, "TaxAmount"))
            });
        }

        foreach (var line in Elements(root, "InvoiceLine").Concat(Elements(root, "CreditNoteLine")))
        {
            var quantity = line.Elements()
                .FirstOrDefault(e => e.Name.LocalName is "InvoicedQuantity" or "CreditedQuantity");

            invoice.Lines.Add(new MudExEInvoiceLine
            {
                Id = Value(line, "ID"),
                Name = Value(line, "Item", "Name"),
                Description = Value(line, "Item", "Description"),
                Quantity = Amount(quantity?.Value),
                Unit = quantity?.Attribute("unitCode")?.Value,
                UnitPrice = Amount(Value(line, "Price", "PriceAmount")),
                TaxPercent = Amount(Value(line, "Item", "ClassifiedTaxCategory", "Percent")),
                LineTotal = Amount(Value(line, "LineExtensionAmount"))
            });
        }

        return invoice;
    }

    private static MudExEInvoiceParty ReadUblParty(XElement party)
    {
        if (party == null)
            return new MudExEInvoiceParty();

        var address = Element(party, "PostalAddress");
        return new MudExEInvoiceParty
        {
            Name = Value(party, "PartyName", "Name") ?? Value(party, "PartyLegalEntity", "RegistrationName"),
            VatId = Elements(party, "PartyTaxScheme")
                .Select(s => Value(s, "CompanyID"))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)),
            Street = Value(address, "StreetName"),
            PostalCode = Value(address, "PostalZone"),
            City = Value(address, "CityName"),
            Country = Value(address, "Country", "IdentificationCode"),
            Email = Value(party, "Contact", "ElectronicMail")
        };
    }

    private static XElement Element(XElement parent, string name)
        => parent?.Elements().FirstOrDefault(e => e.Name.LocalName == name);

    private static IEnumerable<XElement> Elements(XElement parent, string name)
        => parent?.Elements().Where(e => e.Name.LocalName == name) ?? Enumerable.Empty<XElement>();

    private static string Value(XElement parent, params string[] path)
    {
        var current = parent;
        foreach (var name in path)
        {
            current = Element(current, name);
            if (current == null)
                return null;
        }

        var value = current.Value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal? Amount(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static DateTime? Date(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // CII writes 20260131, UBL writes 2026-01-31.
        var formats = new[] { "yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:sszzz" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    }
}
