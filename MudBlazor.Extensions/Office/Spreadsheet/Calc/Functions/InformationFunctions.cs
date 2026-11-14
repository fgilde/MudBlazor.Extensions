namespace MudBlazor.Extensions.Office.Spreadsheet;

internal static class InformationFunctions
{
    private const string Cat = "Information";

    internal static void Register(FunctionRegistry r)
    {
        Is(r, "ISBLANK", "Returns TRUE if the value refers to an empty cell.", v => v.Kind == CellValueKind.Empty);
        Is(r, "ISNUMBER", "Returns TRUE if the value is a number.", v => v.Kind == CellValueKind.Number);
        Is(r, "ISTEXT", "Returns TRUE if the value is text.", v => v.Kind == CellValueKind.Text);
        Is(r, "ISNONTEXT", "Returns TRUE if the value is not text.", v => v.Kind != CellValueKind.Text);
        Is(r, "ISLOGICAL", "Returns TRUE if the value is a logical value.", v => v.Kind == CellValueKind.Boolean);
        Is(r, "ISERROR", "Returns TRUE if the value is any error.", v => v.Kind == CellValueKind.Error);
        Is(r, "ISERR", "Returns TRUE if the value is an error other than #N/A.", v => v.Kind == CellValueKind.Error && v.ErrorValue != CellError.NA);
        Is(r, "ISNA", "Returns TRUE if the value is #N/A.", v => v is { Kind: CellValueKind.Error, ErrorValue: CellError.NA });
        r.Add(Cat, "ISEVEN(number)", "Returns TRUE if the number is even.", Fn.Lift(a => CellValue.Boolean(Math.Truncate(Fn.Num(a[0])) % 2 == 0)));
        r.Add(Cat, "ISODD(number)", "Returns TRUE if the number is odd.", Fn.Lift(a => CellValue.Boolean(Math.Truncate(Fn.Num(a[0])) % 2 != 0)));
        r.Add(Cat, "ISREF(value)", "Returns TRUE if the value is a reference.", (_, a) => CalcValue.Boolean(a[0].Value.Kind == CalcValueKind.Reference));
        r.Add(Cat, "ISFORMULA(reference)", "Returns TRUE if the referenced cell contains a formula.", (_, a) =>
        {
            var value = a[0].Value;
            if (value.Kind != CalcValueKind.Reference) return value.IsError ? value : CalcValue.Error(CellError.Value);
            var (sheet, range) = value.Areas[0];
            return CalcValue.Boolean(sheet[range.Start.Relative]?.Formula != null);
        });
        r.Add(Cat, "NA()", "Returns the error value #N/A.", (_, _) => CalcValue.Error(CellError.NA));
        r.Add(Cat, "N(value)", "Converts a value to a number: numbers stay, TRUE is 1, everything else 0.", Fn.Lift(a => a[0].Kind switch
        {
            CellValueKind.Number or CellValueKind.Error => a[0],
            CellValueKind.Boolean => CellValue.Number(a[0].BooleanValue ? 1 : 0),
            _ => CellValue.Number(0)
        }));
        r.Add(Cat, "TYPE(value)", "Returns the type of a value: 1 number, 2 text, 4 logical, 16 error, 64 array.", (ctx, a) =>
        {
            var value = Fn.Value(a[0]);
            if (Fn.IsMulti(value)) return CalcValue.Number(64);
            return CalcValue.Number(ctx.ToScalar(value).Kind switch
            {
                CellValueKind.Text => 2,
                CellValueKind.Boolean => 4,
                CellValueKind.Error => 16,
                _ => 1
            });
        });
        r.Add(Cat, "ERROR.TYPE(error_val)", "Returns the number of an error value, or #N/A for non-errors.", Fn.Lift(a => a[0].Kind != CellValueKind.Error
            ? CellValue.Error(CellError.NA)
            : a[0].ErrorValue switch
            {
                CellError.Null => CellValue.Number(1),
                CellError.Div0 => CellValue.Number(2),
                CellError.Value => CellValue.Number(3),
                CellError.Ref => CellValue.Number(4),
                CellError.Name => CellValue.Number(5),
                CellError.Num => CellValue.Number(6),
                CellError.NA => CellValue.Number(7),
                CellError.Spill => CellValue.Number(9),
                CellError.Calc => CellValue.Number(14),
                _ => CellValue.Error(CellError.NA)
            }));
        r.Add(Cat, "SHEET([value])", "Returns the sheet number of a reference or sheet name.", (ctx, a) =>
        {
            if (a.Count == 0 || a[0].IsMissing) return CalcValue.Number(IndexOf(ctx, ctx.Sheet));
            var value = a[0].Value;
            if (value.Kind == CalcValueKind.Reference) return CalcValue.Number(IndexOf(ctx, value.Areas[0].Sheet));
            var sheet = ctx.Workbook.FindSheet(Fn.Str(ctx.ToScalar(value)));
            return sheet == null ? CalcValue.Error(CellError.NA) : CalcValue.Number(IndexOf(ctx, sheet));
        });
        r.Add(Cat, "SHEETS([reference])", "Returns the number of sheets in a reference or the workbook.", (ctx, a) =>
        {
            if (a.Count == 0 || a[0].IsMissing) return CalcValue.Number(ctx.Workbook.Sheets.Count);
            var value = a[0].Value;
            return value.Kind == CalcValueKind.Reference ? CalcValue.Number(value.Areas.Select(x => x.Sheet).Distinct().Count()) : CalcValue.Error(CellError.Value);
        });
    }

    private static int IndexOf(FunctionContext ctx, Worksheet sheet) => ctx.Workbook.Sheets.ToList().IndexOf(sheet) + 1;

    private static void Is(FunctionRegistry r, string name, string description, Func<CellValue, bool> test) =>
        r.Add(Cat, name + "(value)", description, Fn.Lift(a => CellValue.Boolean(test(a[0]))));
}
