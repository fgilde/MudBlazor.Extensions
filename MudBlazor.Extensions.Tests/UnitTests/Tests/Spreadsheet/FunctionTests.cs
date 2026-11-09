using System.Globalization;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class FunctionTests
{
    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellValue ValueAt(Worksheet ws, string a1) => ws[R(a1)]?.Value ?? default;

    private static (Workbook Wb, Worksheet Ws, CalcEngine Engine) Fixture(string culture = "en-US")
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var engine = new CalcEngine(wb) { Culture = CultureInfo.GetCultureInfo(culture) };
        string[] fruits = ["apple", "banana", "cherry", "date", "elderberry"];
        for (var i = 0; i < 5; i++)
        {
            wb.Execute(new SetValueCommand(ws, new CellRef(i, 0), CellValue.Number(i + 1)));
            wb.Execute(new SetValueCommand(ws, new CellRef(i, 1), CellValue.Text(fruits[i])));
            wb.Execute(new SetValueCommand(ws, new CellRef(i, 2), CellValue.Number((i + 1) * 10)));
        }

        wb.Execute(new SetValueCommand(ws, R("D1"), CellValue.Boolean(true)));
        wb.Execute(new SetValueCommand(ws, R("D2"), CellValue.Text("x")));
        wb.Execute(new SetValueCommand(ws, R("D3"), CellValue.Error(CellError.Div0)));
        wb.Execute(new SetFormulaCommand(ws, R("E1"), "=A1*2"));
        return (wb, ws, engine);
    }

    private static CellValue Eval(string formula, string culture = "en-US")
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var (wb, ws, _) = Fixture(culture);
            wb.Execute(new SetFormulaCommand(ws, R("Z100"), formula));
            return ValueAt(ws, "Z100");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static void Check(string formula, object expected)
    {
        var v = Eval(formula);
        switch (expected)
        {
            case bool b:
                Assert.Equal(CellValue.Boolean(b), v);
                break;
            case string s when s.StartsWith('#'):
                Assert.Equal(s, v.ToString());
                break;
            case string s:
                Assert.Equal(CellValue.Text(s), v);
                break;
            default:
                Assert.True(v.Kind == CellValueKind.Number, $"{formula} gave {v}");
                var d = Convert.ToDouble(expected, CultureInfo.InvariantCulture);
                Assert.True(Math.Abs(v.NumberValue - d) <= 1e-9 * Math.Max(1, Math.Abs(d)), $"{formula}: expected {d}, got {v.NumberValue}");
                break;
        }
    }

    [Theory]
    [InlineData("=SUM(A1:A5)", 15)]
    [InlineData("=SUM(1,\"2\",TRUE)", 4)]
    [InlineData("=SUM(A1:A5,D3)", "#DIV/0!")]
    [InlineData("=SUM(\"x\")", "#VALUE!")]
    [InlineData("=SUMPRODUCT(A1:A5,C1:C5)", 550)]
    [InlineData("=SUMPRODUCT((A1:A5>2)*C1:C5)", 120)]
    [InlineData("=SUMPRODUCT({1,2},{3})", "#VALUE!")]
    [InlineData("=PRODUCT(A1:A4)", 24)]
    [InlineData("=ROUND(2.675,2)", 2.68)]
    [InlineData("=ROUND(-2.5,0)", -3)]
    [InlineData("=ROUND(1234.5,-2)", 1200)]
    [InlineData("=ROUNDUP(3.141,2)", 3.15)]
    [InlineData("=ROUNDDOWN(-3.149,2)", -3.14)]
    [InlineData("=MROUND(10,3)", 9)]
    [InlineData("=MROUND(5,-2)", "#NUM!")]
    [InlineData("=INT(-2.5)", -3)]
    [InlineData("=TRUNC(-2.5)", -2)]
    [InlineData("=MOD(-3,2)", 1)]
    [InlineData("=MOD(1,0)", "#DIV/0!")]
    [InlineData("=ABS(-4)", 4)]
    [InlineData("=SIGN(-0.5)", -1)]
    [InlineData("=POWER(2,10)", 1024)]
    [InlineData("=SQRT(-1)", "#NUM!")]
    [InlineData("=EXP(1)", Math.E)]
    [InlineData("=LN(0)", "#NUM!")]
    [InlineData("=LOG(8,2)", 3)]
    [InlineData("=LOG10(1000)", 3)]
    [InlineData("=PI()", Math.PI)]
    [InlineData("=CEILING(2.5,1)", 3)]
    [InlineData("=CEILING(-2.5,2)", -2)]
    [InlineData("=CEILING(2.5,-1)", "#NUM!")]
    [InlineData("=CEILING.MATH(-2.5)", -2)]
    [InlineData("=CEILING.MATH(-2.5,1,1)", -3)]
    [InlineData("=FLOOR(2.5,1)", 2)]
    [InlineData("=FLOOR.MATH(-2.5)", -3)]
    [InlineData("=EVEN(1.5)", 2)]
    [InlineData("=ODD(2)", 3)]
    [InlineData("=FACT(5)", 120)]
    [InlineData("=FACT(-1)", "#NUM!")]
    [InlineData("=GCD(24,36)", 12)]
    [InlineData("=LCM(4,6)", 12)]
    [InlineData("=SUBTOTAL(9,A1:A5)", 15)]
    [InlineData("=SUBTOTAL(1,A1:A5)", 3)]
    [InlineData("=AGGREGATE(14,6,A1:A5,2)", 4)]
    [InlineData("=AGGREGATE(9,6,A1:A5,D3)", 15)]
    [InlineData("=AGGREGATE(9,4,A1:A5,D3)", "#DIV/0!")]
    [InlineData("=SIN(PI()/2)", 1)]
    [InlineData("=ATAN2(1,0)", 0)]
    [InlineData("=ATAN2(0,0)", "#DIV/0!")]
    [InlineData("=DEGREES(PI())", 180)]
    [InlineData("=RADIANS(180)", Math.PI)]
    [InlineData("=QUOTIENT(7,2)", 3)]
    [InlineData("=COMBIN(5,2)", 10)]
    [InlineData("=SUMSQ(3,4)", 25)]
    [InlineData("=BASE(255,16)", "FF")]
    [InlineData("=DECIMAL(\"FF\",16)", 255)]
    [InlineData("=ROMAN(1999)", "MCMXCIX")]
    [InlineData("=ARABIC(\"MCMXCIX\")", 1999)]
    [InlineData("=MDETERM({1,2;3,4})", -2)]
    [InlineData("=SUM(MMULT({1,2;3,4},{1;1}))", 10)]
    [InlineData("=ABS(A1:A3)", 1)]
    public void MathFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=AVERAGE(A1:A5)", 3)]
    [InlineData("=AVERAGE(D4:D5)", "#DIV/0!")]
    [InlineData("=AVERAGEA(A1:A2,D1:D2)", 1)]
    [InlineData("=MEDIAN(1,2,3,4)", 2.5)]
    [InlineData("=MODE.SNGL(1,2,2,3)", 2)]
    [InlineData("=MODE.SNGL(1,2,3)", "#N/A")]
    [InlineData("=MIN(A1:A5)", 1)]
    [InlineData("=MAX(C1:C5)", 50)]
    [InlineData("=MINA(D1:D2)", 0)]
    [InlineData("=MAXA(D1:D2)", 1)]
    [InlineData("=COUNT(A1:B5)", 5)]
    [InlineData("=COUNT(1,\"2\",\"x\",TRUE)", 3)]
    [InlineData("=COUNTA(A1:B5)", 10)]
    [InlineData("=COUNTBLANK(A1:D5)", 2)]
    [InlineData("=LARGE(C1:C5,2)", 40)]
    [InlineData("=SMALL(C1:C5,6)", "#NUM!")]
    [InlineData("=RANK.EQ(30,C1:C5)", 3)]
    [InlineData("=RANK.EQ(20,C1:C5,1)", 2)]
    [InlineData("=RANK.EQ(35,C1:C5)", "#N/A")]
    [InlineData("=STDEV.S(A1:A5)", 1.5811388300841898)]
    [InlineData("=STDEV.P(A1:A5)", 1.4142135623730951)]
    [InlineData("=VAR.S(A1:A5)", 2.5)]
    [InlineData("=VAR.P(A1:A5)", 2)]
    [InlineData("=VAR.S(1)", "#DIV/0!")]
    [InlineData("=PERCENTILE.INC(A1:A5,0.25)", 2)]
    [InlineData("=PERCENTILE.INC(A1:A5,1.5)", "#NUM!")]
    [InlineData("=QUARTILE.INC(A1:A5,3)", 4)]
    [InlineData("=CORREL(A1:A5,C1:C5)", 1)]
    [InlineData("=CORREL(A1:A5,{1,2})", "#N/A")]
    [InlineData("=GEOMEAN(2,8)", 4)]
    [InlineData("=SLOPE(C1:C5,A1:A5)", 10)]
    [InlineData("=INTERCEPT(C1:C5,A1:A5)", 0)]
    [InlineData("=FORECAST.LINEAR(6,C1:C5,A1:A5)", 60)]
    [InlineData("=NORM.S.DIST(0,TRUE)", 0.5)]
    [InlineData("=NORM.S.INV(0.975)", 1.959963984540054)]
    [InlineData("=NORM.DIST(1,0,1,FALSE)", 0.24197072451914337)]
    [InlineData("=TRIMMEAN({1,2,3,4,100},0.4)", 3)]
    [InlineData("=SUM(FREQUENCY(A1:A5,{2,4}))", 5)]
    public void StatisticalFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=IF(1>2,\"a\",\"b\")", "b")]
    [InlineData("=IFS(A1>5,\"a\",A1>0,\"b\")", "b")]
    [InlineData("=IFS(FALSE,1)", "#N/A")]
    [InlineData("=IFERROR(1/0,\"z\")", "z")]
    [InlineData("=IFNA(NA(),1)", 1)]
    [InlineData("=IFNA(1/0,1)", "#DIV/0!")]
    [InlineData("=AND(TRUE,1)", true)]
    [InlineData("=AND(TRUE,\"x\")", "#VALUE!")]
    [InlineData("=AND(D1:D2)", true)]
    [InlineData("=AND(D4)", "#VALUE!")]
    [InlineData("=OR(FALSE,0)", false)]
    [InlineData("=XOR(TRUE,TRUE,TRUE)", true)]
    [InlineData("=NOT(0)", true)]
    [InlineData("=TRUE()", true)]
    [InlineData("=FALSE()", false)]
    [InlineData("=SWITCH(2,1,\"a\",2,\"b\")", "b")]
    [InlineData("=SWITCH(3,1,\"a\")", "#N/A")]
    [InlineData("=SWITCH(3,1,\"a\",\"d\")", "d")]
    [InlineData("=LET(x,2,y,x*3,x+y)", 8)]
    [InlineData("=LET(x,2)", "#VALUE!")]
    [InlineData("=SUM(IF(A1:A5>3,C1:C5,0))", 90)]
    public void LogicalFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=CONCAT(B1:B2,\"!\")", "applebanana!")]
    [InlineData("=CONCATENATE(\"a\",1.5,TRUE)", "a1.5TRUE")]
    [InlineData("=TEXTJOIN(\"-\",TRUE,A1:A3,D4)", "1-2-3")]
    [InlineData("=TEXTJOIN(\"-\",FALSE,\"a\",\"\",\"b\")", "a--b")]
    [InlineData("=LEFT(\"hello\",2)", "he")]
    [InlineData("=LEFT(\"hello\",-1)", "#VALUE!")]
    [InlineData("=RIGHT(\"hello\",3)", "llo")]
    [InlineData("=MID(\"hello\",2,3)", "ell")]
    [InlineData("=MID(\"hello\",0,1)", "#VALUE!")]
    [InlineData("=LEN(\"hello\")", 5)]
    [InlineData("=FIND(\"l\",\"hello\")", 3)]
    [InlineData("=FIND(\"L\",\"hello\")", "#VALUE!")]
    [InlineData("=SEARCH(\"L*o\",\"hello\")", 3)]
    [InlineData("=SUBSTITUTE(\"a-b-c\",\"-\",\"+\",2)", "a-b+c")]
    [InlineData("=SUBSTITUTE(\"a-b-c\",\"-\",\"+\")", "a+b+c")]
    [InlineData("=REPLACE(\"abcdef\",2,3,\"X\")", "aXef")]
    [InlineData("=UPPER(\"abc\")", "ABC")]
    [InlineData("=LOWER(\"ABC\")", "abc")]
    [InlineData("=PROPER(\"hello wORLD\")", "Hello World")]
    [InlineData("=TRIM(\"  a   b  \")", "a b")]
    [InlineData("=CLEAN(CHAR(7)&\"a\")", "a")]
    [InlineData("=REPT(\"ab\",3)", "ababab")]
    [InlineData("=TEXT(1234.567,\"#,##0.00\")", "1,234.57")]
    [InlineData("=TEXT(0.25,\"0%\")", "25%")]
    [InlineData("=TEXT(45292,\"yyyy-mm-dd\")", "2024-01-01")]
    [InlineData("=TEXT(45292.5,\"dd.mm.yyyy hh:mm\")", "01.01.2024 12:00")]
    [InlineData("=TEXT(45292,\"dddd, mmmm d\")", "Monday, January 1")]
    [InlineData("=VALUE(\"12.5\")", 12.5)]
    [InlineData("=VALUE(\"50%\")", 0.5)]
    [InlineData("=VALUE(\"abc\")", "#VALUE!")]
    [InlineData("=EXACT(\"a\",\"A\")", false)]
    [InlineData("=CHAR(65)", "A")]
    [InlineData("=CHAR(0)", "#VALUE!")]
    [InlineData("=CODE(\"A\")", 65)]
    [InlineData("=NUMBERVALUE(\"1.234,5\",\",\",\".\")", 1234.5)]
    [InlineData("=TEXTBEFORE(\"a-b-c\",\"-\")", "a")]
    [InlineData("=TEXTBEFORE(\"a-b-c\",\"-\",-1)", "a-b")]
    [InlineData("=TEXTAFTER(\"a-b-c\",\"-\")", "b-c")]
    [InlineData("=TEXTAFTER(\"abc\",\"x\")", "#N/A")]
    [InlineData("=INDEX(TEXTSPLIT(\"a,b,c\",\",\"),2)", "b")]
    [InlineData("=ROWS(TEXTSPLIT(\"a,b;c,d\",\",\",\";\"))", 2)]
    [InlineData("=T(1)", "")]
    [InlineData("=FIXED(1234.567,1)", "1,234.6")]
    public void TextFunctions(string formula, object expected) => Check(formula, expected);

    [Fact]
    public void TextFunctions_FollowEngineCulture()
    {
        Assert.Equal(CellValue.Number(1.5), Eval("=VALUE(\"1,5\")", "de-DE"));
        Assert.Equal(CellValue.Text("1,50"), Eval("=TEXT(1.5,\"0.00\")", "de-DE"));
        Assert.Equal(CellValue.Number(46335), Eval("=DATEVALUE(\"09.11.2026\")", "de-DE"));
        Assert.Equal(CellValue.Number(1234.5), Eval("=NUMBERVALUE(\"1.234,5\")", "de-DE"));
    }

    [Theory]
    [InlineData("=DATE(2024,1,15)", 45306)]
    [InlineData("=DATE(2024,14,1)", 45689)]
    [InlineData("=DATE(1900,2,29)", 60)]
    [InlineData("=DATE(-1,1,1)", "#NUM!")]
    [InlineData("=YEAR(45306)", 2024)]
    [InlineData("=MONTH(45306)", 1)]
    [InlineData("=DAY(60)", 29)]
    [InlineData("=MONTH(60)", 2)]
    [InlineData("=YEAR(-1)", "#NUM!")]
    [InlineData("=HOUR(0.75)", 18)]
    [InlineData("=MINUTE(TIME(12,15,0))", 15)]
    [InlineData("=SECOND(TIME(1,2,3))", 3)]
    [InlineData("=TIME(25,0,0)", 1.0 / 24)]
    [InlineData("=WEEKDAY(45292)", 2)]
    [InlineData("=WEEKDAY(45292,2)", 1)]
    [InlineData("=WEEKDAY(45292,4)", "#NUM!")]
    [InlineData("=WEEKNUM(DATE(2024,1,7))", 2)]
    [InlineData("=ISOWEEKNUM(DATE(2021,1,1))", 53)]
    [InlineData("=EDATE(DATE(2024,1,31),1)", 45351)]
    [InlineData("=EOMONTH(DATE(2024,1,15),1)", 45351)]
    [InlineData("=EOMONTH(DATE(2024,1,15),-1)", 45291)]
    [InlineData("=DATEDIF(DATE(2020,1,15),DATE(2024,3,10),\"Y\")", 4)]
    [InlineData("=DATEDIF(DATE(2020,1,15),DATE(2024,3,10),\"M\")", 49)]
    [InlineData("=DATEDIF(DATE(2020,1,15),DATE(2024,3,10),\"MD\")", 24)]
    [InlineData("=DATEDIF(DATE(2024,1,1),DATE(2023,1,1),\"D\")", "#NUM!")]
    [InlineData("=DAYS(DATE(2024,3,1),DATE(2024,2,1))", 29)]
    [InlineData("=DAYS360(DATE(2024,1,31),DATE(2024,3,31))", 60)]
    [InlineData("=NETWORKDAYS(DATE(2024,1,1),DATE(2024,1,12))", 10)]
    [InlineData("=NETWORKDAYS(DATE(2024,1,1),DATE(2024,1,12),DATE(2024,1,1))", 9)]
    [InlineData("=NETWORKDAYS.INTL(DATE(2024,1,1),DATE(2024,1,7),11)", 6)]
    [InlineData("=WORKDAY(DATE(2024,1,5),1)", 45299)]
    [InlineData("=WORKDAY(DATE(2024,1,8),-1,{45297})", 45296)]
    [InlineData("=DATEVALUE(\"2024-01-15\")", 45306)]
    [InlineData("=DATEVALUE(\"no date\")", "#VALUE!")]
    [InlineData("=TIMEVALUE(\"18:00\")", 0.75)]
    [InlineData("=YEARFRAC(DATE(2024,1,1),DATE(2024,7,1))", 0.5)]
    [InlineData("=YEARFRAC(DATE(2024,1,1),DATE(2024,7,1),1)", 0.4972677595628415)]
    [InlineData("=YEARFRAC(DATE(2024,1,1),DATE(2024,7,1),5)", "#NUM!")]
    public void DateTimeFunctions(string formula, object expected) => Check(formula, expected);

    [Fact]
    public void DateTimeFunctions_TodayAndNow()
    {
        var dayBefore = ExcelDate.ToSerial(DateTime.Today);
        var today = Eval("=TODAY()").NumberValue;
        Assert.Contains(today, new[] { dayBefore, ExcelDate.ToSerial(DateTime.Today) });
        Assert.InRange(Eval("=NOW()").NumberValue - ExcelDate.ToSerial(DateTime.Now), -0.001, 0.001);
    }

    [Theory]
    [InlineData("=VLOOKUP(\"cherry\",B1:C5,2,FALSE)", 30)]
    [InlineData("=VLOOKUP(\"ch*\",B1:C5,2,FALSE)", 30)]
    [InlineData("=VLOOKUP(\"zzz\",B1:C5,2,FALSE)", "#N/A")]
    [InlineData("=VLOOKUP(3.5,A1:C5,3)", 30)]
    [InlineData("=VLOOKUP(3,A1:C5,4)", "#REF!")]
    [InlineData("=VLOOKUP(3,A:C,3,FALSE)", 30)]
    [InlineData("=HLOOKUP(2,{1,2,3;\"a\",\"b\",\"c\"},2,FALSE)", "b")]
    [InlineData("=XLOOKUP(\"date\",B1:B5,C1:C5)", 40)]
    [InlineData("=XLOOKUP(\"zzz\",B1:B5,C1:C5,\"none\")", "none")]
    [InlineData("=XLOOKUP(\"zzz\",B1:B5,C1:C5)", "#N/A")]
    [InlineData("=XLOOKUP(35,C1:C5,B1:B5,,1)", "date")]
    [InlineData("=XLOOKUP(35,C1:C5,B1:B5,,-1)", "cherry")]
    [InlineData("=XMATCH(40,C1:C5)", 4)]
    [InlineData("=XMATCH(4,A1:A5,0,-1)", 4)]
    [InlineData("=MATCH(25,C1:C5)", 2)]
    [InlineData("=MATCH(25,C1:C5,0)", "#N/A")]
    [InlineData("=MATCH(\"b*\",B1:B5,0)", 2)]
    [InlineData("=INDEX(C1:C5,3)", 30)]
    [InlineData("=INDEX(A1:C5,2,3)", 20)]
    [InlineData("=INDEX(A1:C5,6,1)", "#REF!")]
    [InlineData("=SUM(INDEX(A1:C5,0,3))", 150)]
    [InlineData("=SUM(A1:INDEX(A1:A5,3))", 6)]
    [InlineData("=CHOOSE(2,\"a\",\"b\")", "b")]
    [InlineData("=CHOOSE(3,\"a\",\"b\")", "#VALUE!")]
    [InlineData("=LOOKUP(3.5,A1:A5,B1:B5)", "cherry")]
    [InlineData("=SUM(OFFSET(A1,1,0,2,1))", 5)]
    [InlineData("=OFFSET(A1,-1,0)", "#REF!")]
    [InlineData("=INDIRECT(\"C2\")", 20)]
    [InlineData("=SUM(INDIRECT(\"A1:A3\"))", 6)]
    [InlineData("=INDIRECT(\"R2C3\",FALSE)", 20)]
    [InlineData("=INDIRECT(\"nope!!\")", "#REF!")]
    [InlineData("=ROW(C3)", 3)]
    [InlineData("=ROW()", 100)]
    [InlineData("=ROWS(A1:C5)", 5)]
    [InlineData("=COLUMN(C3)", 3)]
    [InlineData("=COLUMNS({1,2,3})", 3)]
    [InlineData("=ADDRESS(2,3)", "$C$2")]
    [InlineData("=ADDRESS(2,3,4)", "C2")]
    [InlineData("=ADDRESS(2,3,1,FALSE)", "R2C3")]
    [InlineData("=ADDRESS(1,1,1,TRUE,\"My Sheet\")", "'My Sheet'!$A$1")]
    [InlineData("=ADDRESS(0,1)", "#VALUE!")]
    [InlineData("=INDEX(TRANSPOSE(A1:A5),1,3)", 3)]
    public void LookupFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=SUMIF(A1:A5,\">2\")", 12)]
    [InlineData("=SUMIF(B1:B5,\"*e*\",C1:C5)", 130)]
    [InlineData("=SUMIFS(C1:C5,A1:A5,\">=2\",A1:A5,\"<=4\")", 90)]
    [InlineData("=SUMIFS(C1:C5,A1:A4,\">1\")", "#VALUE!")]
    [InlineData("=COUNTIF(B1:B5,\"<>apple\")", 4)]
    [InlineData("=COUNTIF(A1:D5,\"\")", 2)]
    [InlineData("=COUNTIF(A:A,\">=3\")", 3)]
    [InlineData("=COUNTIF(A:A,\"\")", 1048571)]
    [InlineData("=COUNTIF(A1:A5,3)", 1)]
    [InlineData("=COUNTIFS(A1:A5,\">1\",C1:C5,\"<50\")", 3)]
    [InlineData("=AVERAGEIF(A1:A5,\">3\")", 4.5)]
    [InlineData("=AVERAGEIF(A1:A5,\">10\")", "#DIV/0!")]
    [InlineData("=AVERAGEIFS(C1:C5,A1:A5,\"<3\")", 15)]
    [InlineData("=MAXIFS(C1:C5,A1:A5,\"<4\")", 30)]
    [InlineData("=MINIFS(C1:C5,B1:B5,\"?a*\")", 20)]
    [InlineData("=COUNTIFS(A1:A5,\">1\",C1:C5)", "#VALUE!")]
    public void ConditionalFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=ISBLANK(D4)", true)]
    [InlineData("=ISBLANK(A1)", false)]
    [InlineData("=ISNUMBER(A1)", true)]
    [InlineData("=ISNUMBER(\"1\")", false)]
    [InlineData("=ISTEXT(B1)", true)]
    [InlineData("=ISERROR(D3)", true)]
    [InlineData("=ISERR(NA())", false)]
    [InlineData("=ISNA(NA())", true)]
    [InlineData("=ISLOGICAL(D1)", true)]
    [InlineData("=ISEVEN(-2)", true)]
    [InlineData("=ISODD(3.7)", true)]
    [InlineData("=ISEVEN(\"x\")", "#VALUE!")]
    [InlineData("=NA()", "#N/A")]
    [InlineData("=TYPE(\"a\")", 2)]
    [InlineData("=TYPE({1,2})", 64)]
    [InlineData("=ERROR.TYPE(1/0)", 2)]
    [InlineData("=ERROR.TYPE(1)", "#N/A")]
    [InlineData("=ISFORMULA(E1)", true)]
    [InlineData("=ISFORMULA(A1)", false)]
    [InlineData("=ISREF(A1)", true)]
    [InlineData("=ISREF(1)", false)]
    [InlineData("=N(TRUE)", 1)]
    public void InformationFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=PMT(0.05/12,360,200000)", -1073.6432460242795)]
    [InlineData("=PMT(0,10,1000)", -100)]
    [InlineData("=PMT(0.1,0,1000)", "#NUM!")]
    [InlineData("=PV(0.05,10,-100)", 772.1734929184817)]
    [InlineData("=FV(0.05,10,-100)", 1257.789253554884)]
    [InlineData("=NPV(0.1,-1000,300,400,500)", -19.124376750222098)]
    [InlineData("=IRR({-1000,300,400,500})", 0.08896339469335)]
    [InlineData("=IRR({1,2})", "#NUM!")]
    [InlineData("=RATE(10,-100,800)", 0.04277497803511126)]
    [InlineData("=NPER(0.05,-100,1000)", 14.206699082890463)]
    [InlineData("=IPMT(0.1,1,3,1000)", -100)]
    [InlineData("=PPMT(0.1,1,3,1000)", -302.1148036253773)]
    [InlineData("=IPMT(0.1,4,3,1000)", "#NUM!")]
    [InlineData("=SLN(1000,100,9)", 100)]
    [InlineData("=EFFECT(0.12,12)", 0.12682503013196977)]
    public void FinancialFunctions(string formula, object expected) => Check(formula, expected);

    [Theory]
    [InlineData("=SUM(SEQUENCE(3,2))", 21)]
    [InlineData("=ROWS(SEQUENCE(4))", 4)]
    [InlineData("=SEQUENCE(0)", "#CALC!")]
    [InlineData("=INDEX(SORT({3;1;2}),1)", 1)]
    [InlineData("=INDEX(SORT({3;1;2},1,-1),1)", 3)]
    [InlineData("=SORT({1;2},2)", "#VALUE!")]
    [InlineData("=ROWS(UNIQUE({1;2;2;3}))", 3)]
    [InlineData("=ROWS(UNIQUE({1;2;2;3},,TRUE))", 2)]
    [InlineData("=SUM(FILTER(A1:A5,A1:A5>2))", 12)]
    [InlineData("=FILTER(A1:A5,A1:A5>9)", "#CALC!")]
    [InlineData("=FILTER(A1:A5,A1:A5>9,\"none\")", "none")]
    [InlineData("=INDEX(SORTBY(B1:B3,C1:C3,-1),1)", "cherry")]
    [InlineData("=COLUMNS(HSTACK({1,2},{3}))", 3)]
    [InlineData("=ROWS(VSTACK({1,2},{3}))", 2)]
    [InlineData("=INDEX(VSTACK({1,2},{3}),2,2)", "#N/A")]
    [InlineData("=SUM(TAKE(A1:A5,-2))", 9)]
    [InlineData("=SUM(DROP(A1:A5,2))", 12)]
    [InlineData("=TAKE(A1:A5,0)", "#CALC!")]
    [InlineData("=ROWS(RANDARRAY(3,2))", 3)]
    [InlineData("=ROWS(WRAPROWS(SEQUENCE(5),2))", 3)]
    [InlineData("=INDEX(CHOOSEROWS(A1:A5,-1),1)", 5)]
    [InlineData("=COLUMNS(TOROW(A1:B2))", 4)]
    public void DynamicArrayFunctions(string formula, object expected) => Check(formula, expected);

    [Fact]
    public void Catalog_HasAtLeast200FunctionsWithInfo()
    {
        var functions = FunctionRegistry.Default.Functions;
        Assert.True(functions.Count >= 200, $"only {functions.Count} functions");
        foreach (var f in functions)
        {
            Assert.Equal(f.Name, f.Info.Name);
            Assert.False(string.IsNullOrWhiteSpace(f.Info.Category), f.Name);
            Assert.StartsWith(f.Name + "(", f.Info.Signature);
            Assert.EndsWith(")", f.Info.Signature);
            Assert.False(string.IsNullOrWhiteSpace(f.Info.Description), f.Name);
        }

        string[] required =
        [
            "SUM", "SUMPRODUCT", "PRODUCT", "ROUND", "ROUNDUP", "ROUNDDOWN", "MROUND", "INT", "TRUNC", "MOD", "ABS", "SIGN", "POWER",
            "SQRT", "EXP", "LN", "LOG", "LOG10", "PI", "RAND", "RANDBETWEEN", "CEILING", "CEILING.MATH", "FLOOR", "FLOOR.MATH", "EVEN",
            "ODD", "FACT", "GCD", "LCM", "SUBTOTAL", "AGGREGATE", "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN", "ATAN2", "DEGREES",
            "RADIANS", "QUOTIENT", "AVERAGE", "AVERAGEA", "MEDIAN", "MODE.SNGL", "MIN", "MAX", "MINA", "MAXA", "COUNT", "COUNTA",
            "COUNTBLANK", "LARGE", "SMALL", "RANK.EQ", "STDEV.S", "STDEV.P", "VAR.S", "VAR.P", "PERCENTILE.INC", "QUARTILE.INC",
            "CORREL", "IF", "IFS", "IFERROR", "IFNA", "AND", "OR", "XOR", "NOT", "TRUE", "FALSE", "SWITCH", "LET", "CONCAT",
            "CONCATENATE", "TEXTJOIN", "LEFT", "RIGHT", "MID", "LEN", "FIND", "SEARCH", "SUBSTITUTE", "REPLACE", "UPPER", "LOWER",
            "PROPER", "TRIM", "CLEAN", "REPT", "TEXT", "VALUE", "EXACT", "CHAR", "CODE", "NUMBERVALUE", "TEXTBEFORE", "TEXTAFTER",
            "TEXTSPLIT", "DATE", "TIME", "TODAY", "NOW", "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "WEEKDAY", "WEEKNUM",
            "ISOWEEKNUM", "EDATE", "EOMONTH", "DATEDIF", "DAYS", "NETWORKDAYS", "WORKDAY", "DATEVALUE", "TIMEVALUE", "YEARFRAC",
            "VLOOKUP", "HLOOKUP", "XLOOKUP", "XMATCH", "INDEX", "MATCH", "CHOOSE", "LOOKUP", "OFFSET", "INDIRECT", "ROW", "ROWS",
            "COLUMN", "COLUMNS", "ADDRESS", "TRANSPOSE", "SUMIF", "SUMIFS", "COUNTIF", "COUNTIFS", "AVERAGEIF", "AVERAGEIFS", "MAXIFS",
            "MINIFS", "ISBLANK", "ISNUMBER", "ISTEXT", "ISERROR", "ISERR", "ISNA", "ISLOGICAL", "ISEVEN", "ISODD", "NA", "TYPE",
            "ERROR.TYPE", "ISFORMULA", "PMT", "PV", "FV", "NPV", "IRR", "RATE", "NPER", "IPMT", "PPMT", "FILTER", "SORT", "SORTBY",
            "UNIQUE", "SEQUENCE", "RANDARRAY", "VSTACK", "HSTACK", "TAKE", "DROP"
        ];
        foreach (var name in required) Assert.True(FunctionRegistry.Default.TryGet(name, out _), name);

        foreach (var name in new[] { "RAND", "RANDARRAY", "RANDBETWEEN", "NOW", "TODAY", "OFFSET", "INDIRECT" })
        {
            Assert.True(FunctionRegistry.Default.TryGet(name, out var f));
            Assert.True(f.Info.IsVolatile, name);
        }

        Assert.True(FunctionRegistry.Default.TryGet("SUM", out var sum));
        Assert.False(sum.Info.IsVolatile);
        Assert.Equal("SUM(number1, [number2], ...)", sum.Info.Signature);
        Assert.Equal("Math", sum.Info.Category);
    }

    [Fact]
    public void ExcelDate_1900LeapYearBug()
    {
        Assert.Equal((1900, 2, 29), ExcelDate.ToDate(60));
        Assert.Equal((1900, 3, 1), ExcelDate.ToDate(61));
        Assert.Equal((1900, 2, 28), ExcelDate.ToDate(59));
        Assert.Equal((1900, 1, 1), ExcelDate.ToDate(1));
        Assert.Equal(60, ExcelDate.FromDate(1900, 2, 29));
        Assert.Equal(61, ExcelDate.FromDate(1900, 3, 1));
        Assert.Equal(new DateTime(1900, 3, 1), ExcelDate.ToDateTime(61));
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), ExcelDate.ToDateTime(45292.5));
        Assert.Equal(45292.5, ExcelDate.ToSerial(new DateTime(2024, 1, 1, 12, 0, 0)));
        Assert.Equal(59, ExcelDate.ToSerial(new DateTime(1900, 2, 28)));
        Assert.Equal(61, ExcelDate.ToSerial(new DateTime(1900, 3, 1)));
    }

    [Fact]
    public void Filter_SpillsIntoNeighbours()
    {
        var (wb, ws, _) = Fixture();
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=FILTER(A1:A5,A1:A5>2)"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "G1"));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "G2"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "G3"));
        Assert.Null(ws[R("G2")].Formula);
        Assert.Equal(R("G1"), ws[R("G3")].SpillAnchor);
        Assert.Null(ws[R("G4")]);

        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(10)));
        Assert.Equal(CellValue.Number(10), ValueAt(ws, "G1"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "G4"));

        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(0)));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "G1"));
        Assert.Null(ws[R("G4")]);

        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SEQUENCE(1,3)"));
        Assert.Null(ws[R("G2")]);
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "I1"));

        wb.Execute(new ClearCommand(ws, new CellRange(R("G1"), R("G1"))));
        Assert.Null(ws[R("H1")]);
        Assert.Null(ws[R("I1")]);
    }

    [Fact]
    public void Spill_BlockedCell_YieldsSpillError()
    {
        var (wb, ws, _) = Fixture();
        wb.Execute(new SetValueCommand(ws, R("G2"), CellValue.Text("x")));
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SEQUENCE(3)"));
        Assert.Equal("#SPILL!", ValueAt(ws, "G1").ToString());
        Assert.Null(ws[R("G3")]);

        wb.Execute(new ClearCommand(ws, new CellRange(R("G2"), R("G2"))));
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "G1"));
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "G2"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "G3"));

        wb.Execute(new SetValueCommand(ws, R("G3"), CellValue.Number(99)));
        Assert.Equal("#SPILL!", ValueAt(ws, "G1").ToString());
        Assert.Null(ws[R("G2")]);
        Assert.Equal(CellValue.Number(99), ValueAt(ws, "G3"));

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(1), ValueAt(ws, "G1"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "G3"));
        Assert.Equal(R("G1"), ws[R("G3")].SpillAnchor);

        wb.Execute(new SetFormulaCommand(ws, R("G1048575"), "=SEQUENCE(5)"));
        Assert.Equal("#SPILL!", ValueAt(ws, "G1048575").ToString());
    }

    [Fact]
    public void SpillRangeReference_Hash()
    {
        var (wb, ws, _) = Fixture();
        Assert.Equal("SUM(G1#)", FormulaParser.Format(FormulaParser.Parse("=sum(g1#)")));
        Assert.Equal("Sheet1!$G$1#", FormulaParser.Format(FormulaParser.Parse("Sheet1!$G$1#")));
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SEQUENCE(3)"));
        wb.Execute(new SetFormulaCommand(ws, R("H1"), "=SUM(G1#)"));
        wb.Execute(new SetFormulaCommand(ws, R("H2"), "=G3*2"));
        wb.Execute(new SetFormulaCommand(ws, R("H3"), "=A1#"));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "H1"));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "H2"));
        Assert.Equal("#REF!", ValueAt(ws, "H3").ToString());

        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SEQUENCE(4,1,2)"));
        Assert.Equal(CellValue.Number(14), ValueAt(ws, "H1"));
        Assert.Equal(CellValue.Number(8), ValueAt(ws, "H2"));

        wb.Execute(new SetFormulaCommand(ws, R("J1"), "=G1#*10"));
        Assert.Equal(CellValue.Number(50), ValueAt(ws, "J4"));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal("SUM(G2#)", ws[R("H2")].Formula);
        Assert.Equal(CellValue.Number(14), ValueAt(ws, "H2"));
        Assert.Null(ws[R("G1")]);
    }

    [Fact]
    public void Volatile_RecalculatesOnEveryChange_AndCalculatedReportsSpillCells()
    {
        var (wb, ws, engine) = Fixture();
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=RAND()"));
        wb.Execute(new SetFormulaCommand(ws, R("H1"), "=SEQUENCE(2)"));
        var before = ValueAt(ws, "G1");
        var seen = new List<CellChange>();
        engine.Calculated += (_, e) => seen.AddRange(e.Cells);
        wb.Execute(new SetValueCommand(ws, R("Y1"), CellValue.Number(1)));
        Assert.NotEqual(before, ValueAt(ws, "G1"));
        Assert.Contains(new CellChange(ws, R("G1")), seen);

        seen.Clear();
        wb.Execute(new SetFormulaCommand(ws, R("H1"), "=SEQUENCE(3)"));
        Assert.Contains(new CellChange(ws, R("H1")), seen);
        Assert.Contains(new CellChange(ws, R("H3")), seen);
        Assert.Equal(new CellRange(R("H1"), R("H3")), engine.SpillRange(ws, R("H1")));
    }

    [Fact]
    public void FreedSpillCells_RecalculateTheirReaders()
    {
        var (wb, ws, _) = Fixture();
        wb.Execute(new SetFormulaCommand(ws, R("H2"), "=G3*2"));
        wb.Execute(new SetFormulaCommand(ws, R("G1"), "=SEQUENCE(3)"));
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "H2"));
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "H2"));
        wb.Undo.Redo();
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "H2"));
        wb.Execute(new ClearCommand(ws, new CellRange(R("G1"), R("G1"))));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "H2"));
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(6), ValueAt(ws, "H2"));
        wb.Execute(new SetValueCommand(ws, R("G1"), CellValue.Number(5)));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "H2"));
    }

    [Fact]
    public void VolatileReferences_SeeEditedPrecedentFormulas()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb);
        wb.Execute(new SetValueCommand(ws, R("X1"), CellValue.Number(1)));
        wb.Execute(new SetFormulaCommand(ws, R("A3"), "=X1*2"));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=SUM(OFFSET(A1,0,0,5))"));
        wb.Execute(new SetFormulaCommand(ws, R("C1"), "=INDIRECT(\"A3\")+0"));
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "B1"));
        wb.Execute(new SetFormulaCommand(ws, R("A3"), "=X1*3"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "C1"));
        wb.Execute(new SetValueCommand(ws, R("X1"), CellValue.Number(10)));
        Assert.Equal(CellValue.Number(30), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(30), ValueAt(ws, "C1"));
    }

    [Theory]
    [InlineData("=VLOOKUP(3.5,A1:C5,3,)", "#N/A")]
    [InlineData("=VLOOKUP(3,A1:C5,3,)", 30)]
    [InlineData("=HLOOKUP(1.5,A1:C2,2,)", "#N/A")]
    [InlineData("=GCD(Y1:Y2)", "#VALUE!")]
    [InlineData("=LCM(Y1:Y2)", "#VALUE!")]
    [InlineData("=YEAR(2958466)", "#NUM!")]
    [InlineData("=YEAR(2958465)", 9999)]
    [InlineData("=DATEVALUE(\"18:00\")", 0)]
    [InlineData("=DAY(\"18:00\")", 0)]
    [InlineData("=HOUR(\"18:00\")", 18)]
    [InlineData("=DATEVALUE(\"1899-12-30\")", "#VALUE!")]
    [InlineData("=VALUE(\"1899-12-30\")", "#VALUE!")]
    [InlineData("=YEAR(\"1899-12-30\")", "#VALUE!")]
    [InlineData("=DATEVALUE(\"1900-03-01\")", 61)]
    [InlineData("=MODE(3,1,1,3,2)", 3)]
    [InlineData("=MODE(1,2,3)", "#N/A")]
    public void ReviewFixes_LookupDateAndDivisorEdgeCases(string formula, object expected) => Check(formula, expected);

    [Fact]
    public void Lookup_TrimsOnlyAlongTheSearchAxis()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        _ = new CalcEngine(wb);
        for (var i = 0; i < 3; i++)
        {
            wb.Execute(new SetValueCommand(ws, new CellRef(i, 0), CellValue.Number(i + 1)));
            wb.Execute(new SetValueCommand(ws, new CellRef(i, 2), CellValue.Text("xyz"[i].ToString())));
        }

        wb.Execute(new SetFormulaCommand(ws, R("E20"), "=LOOKUP(2,A1:D10)"));
        wb.Execute(new SetFormulaCommand(ws, R("E21"), "=VLOOKUP(3,A1:F5,6,FALSE)"));
        wb.Execute(new SetFormulaCommand(ws, R("E22"), "=VLOOKUP(3,A1:F5,7,FALSE)"));
        wb.Execute(new SetFormulaCommand(ws, R("E23"), "=HLOOKUP(1,A1:C30,30,FALSE)"));
        wb.Execute(new SetFormulaCommand(ws, R("E24"), "=VLOOKUP(\"y\",C1:C3,1,FALSE)"));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "E20"));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "E21"));
        Assert.Equal(CellValue.Error(CellError.Ref), ValueAt(ws, "E22"));
        Assert.Equal(CellValue.Number(0), ValueAt(ws, "E23"));
        Assert.Equal(CellValue.Text("y"), ValueAt(ws, "E24"));
    }
}
