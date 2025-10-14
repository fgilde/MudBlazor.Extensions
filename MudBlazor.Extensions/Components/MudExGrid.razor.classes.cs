namespace MudBlazor.Extensions.Components;

public record SectionLayoutDto(string Id, int Row, int Column, int RowSpan, int ColSpan);
public record GridLayoutDto(int Columns, int Rows, List<SectionLayoutDto> Sections);

