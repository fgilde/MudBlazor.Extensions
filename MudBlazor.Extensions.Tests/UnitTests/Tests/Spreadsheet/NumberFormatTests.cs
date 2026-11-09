using System.Globalization;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class NumberFormatTests
{
    private static CultureInfo C(string name) => CultureInfo.GetCultureInfo(name);

    private static string F(double value, string format, string culture = "en-US") =>
        NumberFormat.Format(CellValue.Number(value), format, C(culture), out _);

    [Theory]
    [InlineData("en-US", "1,234.50")]
    [InlineData("de-DE", "1.234,50")]
    public void Thousands_And_Decimals_FollowCulture(string culture, string expected) =>
        Assert.Equal(expected, F(1234.5, "#,##0.00", culture));

    [Theory]
    [InlineData("en-US", "12%", "12.50%")]
    [InlineData("de-DE", "12%", "12,50%")]
    public void Percent(string culture, string whole, string decimals)
    {
        Assert.Equal(whole, F(0.12, "0%", culture));
        Assert.Equal(decimals, F(0.125, "0.00%", culture));
    }

    [Theory]
    [InlineData("en-US", "1.23E+03", "1.23E-03")]
    [InlineData("de-DE", "1,23E+03", "1,23E-03")]
    public void Scientific(string culture, string big, string small)
    {
        Assert.Equal(big, F(1234.5, "0.00E+00", culture));
        Assert.Equal(small, F(0.0012345, "0.00E+00", culture));
        Assert.Equal("0.00E+00", F(0, "0.00E+00"));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Colors_FollowTheChosenSection(string culture)
    {
        Assert.Equal("-5", NumberFormat.Format(CellValue.Number(5), "[Red]-0;0", C(culture), out var color));
        Assert.Equal("Red", color);
        Assert.Equal("5", NumberFormat.Format(CellValue.Number(-5), "[Red]-0;0", C(culture), out color));
        Assert.Null(color);
        Assert.Equal("-5", NumberFormat.Format(CellValue.Number(-5), "0;[Red]-0", C(culture), out color));
        Assert.Equal("Red", color);
        Assert.Equal("5", NumberFormat.Format(CellValue.Number(5), "[Blue]0;[Red]-0", C(culture), out color));
        Assert.Equal("Blue", color);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Dates_And_Times(string culture)
    {
        Assert.Equal("09.11.2026 18:30", F(46335.7708333333, "dd.mm.yyyy hh:mm", culture));
        Assert.Equal("36:00", F(1.5, "[h]:mm", culture));
        Assert.Equal("2026-11-09", F(46335, "yyyy-mm-dd", culture));
        Assert.Equal("6:30 PM", F(46335.7708333333, "h:mm AM/PM", culture));
        Assert.Equal("18:30:00", F(46335.7708333333, "hh:mm:ss", culture));
    }

    [Fact]
    public void Dates_UseCultureNamesAndFractionalSeconds()
    {
        Assert.Equal("Monday, November 9, 26", F(46335, "dddd, mmmm d, yy"));
        Assert.Equal("Montag, 9. November", F(46335, "dddd, d. mmmm", "de-DE"));
        Assert.Equal("00:01.5", F(1.5 / 86400, "mm:ss.0"));
        Assert.Equal("00:01,5", F(1.5 / 86400, "mm:ss.0", "de-DE"));
        Assert.Equal("29.02.1900", F(60, "dd.mm.yyyy"));
        Assert.Equal("########", F(-1, "dd.mm.yyyy"));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Fractions(string culture)
    {
        Assert.Equal("3/4", F(0.75, "# ?/?", culture));
        Assert.Equal("1 1/2", F(1.5, "# ?/?", culture));
        Assert.Equal("2  1/3 ", F(2.3333, "# ??/??", culture));
        Assert.Equal("3    ", F(3, "# ?/?", culture));
        Assert.Equal("5/8", F(0.625, "?/8", culture));
    }

    [Fact]
    public void Sections_Literals_And_Text()
    {
        Assert.Equal("(5.00)", F(-5, "0.00;(0.00)"));
        Assert.Equal("zero", F(0, "0;-0;\"zero\""));
        Assert.Equal("$1,234", F(1234, "$#,##0"));
        Assert.Equal("-$5", F(-5, "$#,##0"));
        Assert.Equal("5 kg", F(5, "0 \"kg\""));
        Assert.Equal("5 m", F(5, "0 \\m"));
        Assert.Equal("5 ", F(5, "0_)"));
        Assert.Equal("5", F(5, "**0"));
        Assert.Equal("big", F(150, "[>100]\"big\";0"));
        Assert.Equal("7", F(7, "[>100]\"big\";0"));
        Assert.Equal("1,235", F(1234567, "#,##0,"));
        Assert.Equal("007", F(7, "000"));
        Assert.Equal(".5", F(0.5, "#.##"));
        Assert.Equal("1.01", F(1.005, "0.00"));
        Assert.Equal("12.50", F(12.5, ".00"));
        Assert.Equal("12.3E+3", F(12345, "##0.0E+0"));
        Assert.Equal("5", F(5, "@"));
        Assert.Equal("€5.00", F(5, "[$€-407]0.00"));
        Assert.Equal("-0", F(-0.4, "0"));
        Assert.Equal("Name: abc", NumberFormat.Format(CellValue.Text("abc"), "0;-0;0;\"Name: \"@", C("en-US"), out _));
        Assert.Equal("abc", NumberFormat.Format(CellValue.Text("abc"), "0.00", C("en-US"), out _));
        Assert.Equal("", NumberFormat.Format(CellValue.Empty, "0.00", C("en-US"), out _));
        Assert.Equal("#DIV/0!", NumberFormat.Format(CellValue.Error(CellError.Div0), "0.00", C("en-US"), out _));
        Assert.Equal("TRUE", NumberFormat.Format(CellValue.Boolean(true), "0", C("en-US"), out _));
        Assert.Equal("WAHR", NumberFormat.Format(CellValue.Boolean(true), "0", C("de-DE"), out _));
    }

    [Theory]
    [InlineData(1234.5, "1234.5")]
    [InlineData(1.0 / 3, "0.333333333")]
    [InlineData(123456789012, "1.23457E+11")]
    [InlineData(-0.5, "-0.5")]
    [InlineData(0, "0")]
    public void General(double value, string expected) => Assert.Equal(expected, F(value, "General"));

    [Fact]
    public void BuiltInFormats()
    {
        Assert.Equal("General", BuiltInNumberFormats.Get(0));
        Assert.Equal("#,##0.00", BuiltInNumberFormats.Get(4));
        Assert.Equal("[h]:mm:ss", BuiltInNumberFormats.Get(46));
        Assert.Equal("@", BuiltInNumberFormats.Get(49));
        Assert.Null(BuiltInNumberFormats.Get(100));
        Assert.Equal("dd.mm.yyyy", BuiltInNumberFormats.Get(14, C("de-DE")));
        Assert.True(BuiltInNumberFormats.TryGetId("0.00%", out var id));
        Assert.Equal(10, id);
        Assert.True(BuiltInNumberFormats.TryGetId("dd.mm.yyyy", out id, C("de-DE")));
        Assert.Equal(14, id);
        Assert.False(BuiltInNumberFormats.TryGetId("0.000", out _));
    }

    [Theory]
    [InlineData("en-US", "1.5", 1.5, null)]
    [InlineData("de-DE", "1,5", 1.5, null)]
    [InlineData("en-US", "1,234.5", 1234.5, "#,##0.00")]
    [InlineData("de-DE", "1.234", 1234, "#,##0")]
    [InlineData("en-US", "12%", 0.12, "0%")]
    [InlineData("de-DE", "12%", 0.12, "0%")]
    [InlineData("de-DE", "12,5%", 0.125, "0.00%")]
    [InlineData("en-US", "-7", -7, null)]
    [InlineData("en-US", "(7)", -7, null)]
    [InlineData("en-US", "$1,234.50", 1234.5, "$#,##0.00")]
    [InlineData("de-DE", "5 €", 5, "#,##0 \"€\"")]
    [InlineData("en-US", "1.5E+3", 1500, "0.00E+00")]
    [InlineData("de-DE", "09.11.2026", 46335, "dd.mm.yyyy")]
    [InlineData("en-US", "11/9/2026", 46335, "m/d/yyyy")]
    [InlineData("en-US", "2026-11-09", 46335, "m/d/yyyy")]
    [InlineData("de-DE", "2026-11-09 18:00", 46335.75, "dd.mm.yyyy hh:mm")]
    [InlineData("en-US", "18:00", 0.75, "h:mm")]
    [InlineData("en-US", "6:00 PM", 0.75, "h:mm AM/PM")]
    [InlineData("de-DE", "18:00:30", 0.75 + 30 / 86400.0, "hh:mm:ss")]
    [InlineData("en-US", "25:00", 25 / 24.0, "[h]:mm:ss")]
    [InlineData("en-US", "9-Nov-2026", 46335, "m/d/yyyy")]
    [InlineData("de-DE", "9. November 2026", 46335, "dd.mm.yyyy")]
    [InlineData("en-US", "Nov 2026", 46327, "mmm-yy")]
    public void Input_Numbers_And_Dates(string culture, string input, double expected, string format)
    {
        var (value, suggested) = CellInputParser.Parse(input, C(culture));
        Assert.Equal(CellValueKind.Number, value.Kind);
        Assert.Equal(expected, value.NumberValue, 9);
        Assert.Equal(format, suggested);
    }

    [Fact]
    public void Input_YearlessDate_UsesCurrentYear()
    {
        var year = DateTime.Today.Year;
        var (value, format) = CellInputParser.Parse("9.11", C("de-DE"));
        Assert.Equal(ExcelDate.FromDate(year, 11, 9), value.NumberValue);
        Assert.Equal("d-mmm", format);
        Assert.Equal(ExcelDate.FromDate(year, 1, 5), CellInputParser.Parse("1/5", C("en-US")).Value.NumberValue);
        Assert.Equal(ExcelDate.FromDate(year, 1, 5), CellInputParser.Parse("Jan 5", C("en-US")).Value.NumberValue);
    }

    [Theory]
    [InlineData("en-US", "'0123", "0123")]
    [InlineData("en-US", "abc", "abc")]
    [InlineData("en-US", "1,5", "1,5")]
    [InlineData("en-US", "1.2.3", "1.2.3")]
    [InlineData("de-DE", "31.02.2026", "31.02.2026")]
    [InlineData("en-US", "'=A1", "=A1")]
    public void Input_Text(string culture, string input, string expected)
    {
        var (value, format) = CellInputParser.Parse(input, C(culture));
        Assert.Equal(CellValue.Text(expected), value);
        Assert.Null(format);
    }

    [Fact]
    public void Input_BooleansErrorsAndEmpty()
    {
        Assert.Equal(CellValue.Boolean(true), CellInputParser.Parse("true", C("en-US")).Value);
        Assert.Equal(CellValue.Boolean(false), CellInputParser.Parse("FALSCH", C("de-DE")).Value);
        Assert.Equal(CellValue.Boolean(true), CellInputParser.Parse("WAHR", C("de-DE")).Value);
        Assert.Equal(CellValue.Text("WAHR"), CellInputParser.Parse("WAHR", C("en-US")).Value);
        Assert.Equal(CellValue.Error(CellError.NA), CellInputParser.Parse("#N/A", C("en-US")).Value);
        Assert.Equal(CellValue.Empty, CellInputParser.Parse("", C("en-US")).Value);
    }

    private static CellValue Eval(string formula, string culture)
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb) { Culture = C(culture) };
        wb.Execute(new SetFormulaCommand(ws, CellRef.Parse("A1"), formula));
        return ws[CellRef.Parse("A1")].Value;
    }

    [Fact]
    public void Engine_UsesEngineCultureForAllConversions()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = C("en-US");
            Assert.Equal(CellValue.Number(2.5), Eval("=\"1,5\"+1", "de-DE"));
            Assert.Equal(CellValue.Text("x1,5"), Eval("=\"x\"&1.5", "de-DE"));
            Assert.Equal(CellValue.Number(0.12), Eval("=\"12%\"*1", "de-DE"));
            Assert.Equal(CellValue.Number(46336), Eval("=\"09.11.2026\"+1", "de-DE"));
            Assert.Equal(CellValue.Number(2), Eval("=COUNTIF({1.5,2.5,3},\">1,5\")", "de-DE"));
            Assert.Equal(CellValue.Text("1.234,50"), Eval("=TEXT(1234.5,\"#,##0.00\")", "de-DE"));
            Assert.Equal(CellValue.Error(CellError.Value), Eval("=\"TRUE\"+0", "en-US"));
            Assert.Equal(CellValue.Number(-5), Eval("=VALUE(\"(5)\")", "en-US"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void DateText_WithoutYear_UsesCurrentYear()
    {
        var year = DateTime.Today.Year;
        Assert.Equal(CellValue.Number(ExcelDate.FromDate(year, 1, 5)), Eval("=DATEVALUE(\"Jan 5\")", "en-US"));
        Assert.Equal(CellValue.Number(ExcelDate.FromDate(year, 1, 5)), Eval("=DATEVALUE(\"1/5\")", "en-US"));
        Assert.Equal(CellValue.Number(ExcelDate.FromDate(year, 1, 5)), Eval("=VALUE(\"1/5\")", "en-US"));
        Assert.Equal(CellValue.Number(0), Eval("=DATEVALUE(\"18:00\")", "en-US"));
        Assert.Equal(CellValue.Number(0.75), Eval("=TIMEVALUE(\"2026-11-09 18:00\")", "en-US"));
        Assert.Equal(CellValue.Error(CellError.Value), Eval("=TIMEVALUE(\"12\")", "en-US"));
        Assert.Equal(CellValue.Error(CellError.Value), Eval("=TEXT(-1,\"yyyy\")", "en-US"));
    }
}
