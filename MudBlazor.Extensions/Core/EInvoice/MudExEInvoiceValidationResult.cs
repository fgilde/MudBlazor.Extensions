namespace MudBlazor.Extensions.Core.EInvoice;

/// <summary>
/// What checking an invoice found.
/// </summary>
public class MudExEInvoiceValidationResult
{
    /// <summary>Everything the check found, errors and warnings together.</summary>
    public List<MudExEInvoiceViolation> Violations { get; } = new();

    /// <summary>True when nothing was found that makes the invoice unusable.</summary>
    public bool IsValid => Violations.All(v => v.Severity != MudExEInvoiceSeverity.Error);

    /// <summary>How many rules were broken outright.</summary>
    public int ErrorCount => Violations.Count(v => v.Severity == MudExEInvoiceSeverity.Error);

    /// <summary>How many things are worth a look without being wrong.</summary>
    public int WarningCount => Violations.Count(v => v.Severity == MudExEInvoiceSeverity.Warning);

    /// <summary>Adds one finding.</summary>
    public void Add(string rule, string message, MudExEInvoiceSeverity severity)
        => Violations.Add(new MudExEInvoiceViolation(rule, message, severity));

    /// <summary>A short sentence for a chip or a log line.</summary>
    public override string ToString() => Violations.Count == 0
        ? "No findings"
        : $"{ErrorCount} errors, {WarningCount} warnings";
}

/// <summary>One broken rule.</summary>
/// <param name="Rule">Identifier of the rule, like BR-CO-15.</param>
/// <param name="Message">What is wrong, in words.</param>
/// <param name="Severity">Whether it makes the invoice unusable.</param>
public record MudExEInvoiceViolation(string Rule, string Message, MudExEInvoiceSeverity Severity);

/// <summary>How bad a finding is.</summary>
public enum MudExEInvoiceSeverity
{
    /// <summary>Worth knowing, but the invoice can be processed.</summary>
    Warning,

    /// <summary>A rule of the standard is broken.</summary>
    Error
}
