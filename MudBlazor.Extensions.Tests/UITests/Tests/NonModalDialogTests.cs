using FluentAssertions;
using Microsoft.Playwright;

namespace MudBlazor.Extensions.Tests.UITests.Tests;

/// <summary>
/// Non-modal dialogs, covering what was reported in #42 (closing in another order than they were
/// opened breaks the remaining dialogs), #125 (closing right after opening leaves the dialog stuck)
/// and #204 (a modal dialog renders behind an open non-modal one).
/// </summary>
[Collection(PlaywrightFixture.PlaywrightCollection)]
public class NonModalDialogTests : BaseUITest
{
    protected override Browser? TargetBrowser => Browser.Chromium;

    public NonModalDialogTests(PlaywrightFixture playwrightFixture) : base(playwrightFixture)
    { }

    private static ILocator NonModalButton(IPage page) => page.Locator("[data-testid=open-non-modal]");
    private static ILocator ModalButton(IPage page) => page.Locator("[data-testid=open-modal]");
    private static ILocator Dialog(IPage page, string marker) => page.Locator($".mud-dialog:has([data-marker='{marker}'])");
    private static ILocator CloseButton(ILocator dialog) => dialog.Locator(".mud-button-close");

    private async Task OpenPage(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await NonModalButton(page).WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    private static async Task<int> OpenDialogCount(IPage page)
        => await page.Locator(".mud-dialog").CountAsync();

    /// <summary>
    /// Drags the dialog by its header and reports whether it actually moved.
    /// </summary>
    private static async Task<bool> DragBy(IPage page, ILocator dialog, int dx, int dy)
    {
        var before = await dialog.BoundingBoxAsync();
        var header = dialog.Locator(".mud-dialog-title");
        var headerBox = await header.BoundingBoxAsync();
        if (before is null || headerBox is null) return false;

        // grab the header left of the buttons
        var startX = headerBox.X + 20;
        var startY = headerBox.Y + headerBox.Height / 2;
        await page.Mouse.MoveAsync(startX, startY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(startX + dx / 2, startY + dy / 2, new() { Steps = 5 });
        await page.Mouse.MoveAsync(startX + dx, startY + dy, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await Task.Delay(500);

        var after = await dialog.BoundingBoxAsync();
        if (after is null) return false;
        return Math.Abs(after.X - before.X) > 5 || Math.Abs(after.Y - before.Y) > 5;
    }

    [Fact]
    public async Task Closing_the_first_opened_dialog_leaves_the_second_one_usable()
    {
        await Test($"{Url}/no-modal", async page =>
        {
            await OpenPage(page);

            await NonModalButton(page).ClickAsync();
            await Dialog(page, "non-modal-1").WaitForAsync();
            await NonModalButton(page).ClickAsync();
            await Dialog(page, "non-modal-2").WaitForAsync();
            await Task.Delay(1500); // dialogs animate in

            // close the older one first, this is the order that used to break the remaining dialog
            await CloseButton(Dialog(page, "non-modal-1")).ClickAsync();
            await Task.Delay(1000);

            (await Dialog(page, "non-modal-1").CountAsync()).Should().Be(0, "the first dialog was closed");
            (await Dialog(page, "non-modal-2").CountAsync()).Should().Be(1, "the second dialog stays open");

            // the remaining dialog must still be draggable, this is what broke after the reinit
            var remaining = Dialog(page, "non-modal-2");
            var moved = await DragBy(page, remaining, 60, 40);
            moved.Should().BeTrue("the remaining dialog can still be dragged by its header");

            // and it must still close on a single click
            await CloseButton(remaining).ClickAsync();
            await Task.Delay(1000);

            (await OpenDialogCount(page)).Should().Be(0, "no dialog reappears and none is left behind");
        });
    }

    [Fact]
    public async Task Closing_right_after_opening_closes_the_dialog()
    {
        await Test($"{Url}/no-modal", async page =>
        {
            await OpenPage(page);

            await NonModalButton(page).ClickAsync();
            var dialog = Dialog(page, "non-modal-1");
            await dialog.WaitForAsync();

            // no waiting for the open animation here, that is the reported case
            await CloseButton(dialog).ClickAsync();
            await Task.Delay(2000);

            (await OpenDialogCount(page)).Should().Be(0, "the dialog closes even when closed immediately");
        });
    }

    [Fact]
    public async Task A_modal_dialog_opens_on_top_of_a_non_modal_one()
    {
        await Test($"{Url}/no-modal", async page =>
        {
            await OpenPage(page);

            await NonModalButton(page).ClickAsync();
            await Dialog(page, "non-modal-1").WaitForAsync();
            await Task.Delay(1500);

            // the demo places dialogs randomly, move it over the viewport centre so it really
            // overlaps the modal dialog that opens there
            await page.EvaluateAsync(@"() => {
                const d = document.querySelector('[data-marker=""non-modal-1""]').closest('.mud-dialog');
                d.style.left = (window.innerWidth / 2 - d.offsetWidth / 2) + 'px';
                d.style.top = (window.innerHeight / 2 - d.offsetHeight / 2) + 'px';
            }");

            await ModalButton(page).ClickAsync();
            await Dialog(page, "modal").WaitForAsync();
            await Task.Delay(1500);

            var onTop = await page.EvaluateAsync<string>(@"() => {
                const byMarker = m => document.querySelector('[data-marker=""' + m + '""]')?.closest('.mud-dialog');
                const modal = byMarker('modal');
                const nonModal = byMarker('non-modal-1');
                if (!modal || !nonModal) return 'missing dialog';
                const r = modal.getBoundingClientRect();
                const n = nonModal.getBoundingClientRect();
                const overlaps = r.left < n.right && n.left < r.right && r.top < n.bottom && n.top < r.bottom;
                if (!overlaps) return 'dialogs do not overlap';
                const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
                if (modal.contains(hit)) return 'modal';
                if (nonModal.contains(hit)) return 'non-modal';
                return hit ? hit.tagName + '.' + hit.className : 'nothing';
            }");

            onTop.Should().Be("modal", "a modal dialog must cover an already open non-modal dialog");
        });
    }
}
