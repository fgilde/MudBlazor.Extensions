# Task 13 report: slide show, design and table demos

Status: DONE

## New routes (browser check)
- `/slides-show` "Slide Show" (DemoNew, Group "Slide Editor", Order 10)
  1. SlidesShowExample: editor with the full demo deck; F5 / Shift+F5 / Slide Show tab / status bar button; transitions (Fade on slides 2..n) and 3 click-built shapes on the last slide.
  2. SlidesPresenterExample: same editor; enable Presenter View in the Slide Show tab, start the show (popup, notes, next slide, clock, stopwatch).
  3. SlidesStandaloneShowExample: `MudExSlideShow` without editor; "Start the show" button, "Presenter view" switch, Closed removes the show (Esc ends).
- `/slides-design` "Design and Tables" (DemoNew, Order 15)
  1. SlidesDesignExample: editor on the Ocean design; buttons apply each of the six built-in designs via `ApplyDesignCommand` (undoable in the editor); Design tab as well.
  2. SlidesTablesExample: tables deck (title slide + slide with 5x3 table, last row merged across 3 columns); Table Design / Layout tabs.
- `/slides`: tile row extended (slide show, designs, tables, groups and sections).

## Deck (`SlidesSamples.CreateDeck(SlideDesign design = null)`, new `CreateTablesDeck()`)
7 slides: original 4 plus Tables (4x3 table), Groups (ellipse + arrow grouped), Build steps (3 entrance effects: Fade, Fly In, Zoom); sections "Default Section" (slide 1), "Content" (2-4), "Tables and motion" (5-7); Fade transition on slides 2-7; notes on every slide. No chart slide (no chart support in the model).

## Tests
- `SlidesSamples_FullDeckRoundTrips` (table, group, sections cover all slides, transition, animations), `SlidesSamples_DesignDeck_UsesTheDesign`: written first (red: compile error), now green.
- `OfficeDemoConventionsTests`: new examples are picked up automatically; the standalone show example is clicked first (`StartsOnClick`) and expects `MudExSlideShow` as ComponentRef.
- Filter Slide|Sample|Demo|Presentation: 494/494. Build 0 errors.

## Notes
- Standalone example sets `ReplacesComponentRef = true` (ComponentRef is the show, only present while running) and uses `@ref="ComponentRef"`.
- Generated example-codes (.md/.html) committed. index.html restored. User's two uncommitted files untouched and not staged.
