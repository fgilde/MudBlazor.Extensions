using System.Globalization;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExCronScheduleTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static string Describe(string expression) => MudExCronSchedule.Parse(expression).Describe((key, args) => string.Format(English, key, args), English);

    [Theory]
    [InlineData("*/15 * * * *", MudExCronFrequency.Minutes, "Every 15 minutes")]
    [InlineData("30 * * * *", MudExCronFrequency.Hourly, "Every hour at minute 30")]
    [InlineData("0 6 * * *", MudExCronFrequency.Daily, "Every day at 06:00 UTC")]
    [InlineData("5 18 * * 1-5", MudExCronFrequency.Weekly, "Every Monday, Tuesday, Wednesday, Thursday, Friday at 18:05 UTC")]
    [InlineData("0 7 * * 0,6", MudExCronFrequency.Weekly, "Every Sunday, Saturday at 07:00 UTC")]
    [InlineData("0 3 15 * *", MudExCronFrequency.Monthly, "Every month on day 15 at 03:00 UTC")]
    [InlineData("0 6 * * MON", MudExCronFrequency.Custom, "Custom schedule")]
    [InlineData("0 0 1 1 *", MudExCronFrequency.Custom, "Custom schedule")]
    [InlineData("*/7 3-5 * * 1", MudExCronFrequency.Custom, "Custom schedule")]
    public void Recognizes_and_describes_the_common_patterns(string expression, MudExCronFrequency frequency, string description)
    {
        Assert.Equal(frequency, MudExCronSchedule.Parse(expression).Frequency);
        Assert.Equal(description, Describe(expression));
    }

    [Theory]
    [InlineData("*/15 * * * *")]
    [InlineData("30 * * * *")]
    [InlineData("0 6 * * *")]
    [InlineData("5 18 * * 1,2,3,4,5")]
    [InlineData("0 3 15 * *")]
    public void Builds_the_expression_it_parsed(string expression) => Assert.Equal(expression, MudExCronSchedule.Parse(expression).ToExpression());

    [Fact]
    public void Sunday_as_7_is_sunday_and_custom_keeps_no_expression()
    {
        Assert.Equal([DayOfWeek.Sunday], MudExCronSchedule.Parse("0 6 * * 7").Weekdays);
        Assert.Null(MudExCronSchedule.Parse("0 0 1 1 *").ToExpression());
        Assert.Equal("30 9 * * 1,5", new MudExCronSchedule(MudExCronFrequency.Weekly, Minute: 30, Hour: 9, Days: [DayOfWeek.Friday, DayOfWeek.Monday]).ToExpression());
    }

    [Theory]
    [InlineData("0 6 * * *", true)]
    [InlineData("0 0 6 * * *", true)]
    [InlineData("*/5 9-17 * JAN-MAR MON-FRI", true)]
    [InlineData("0 12 ? * 5#3", true)]
    [InlineData("60 * * * *", false)]
    [InlineData("0 24 * * *", false)]
    [InlineData("0 6 0 * *", false)]
    [InlineData("0 6 * * * * *", false)]
    [InlineData("every day", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validates_expressions(string expression, bool valid) => Assert.Equal(valid, MudExCronSchedule.IsValid(expression));
}
