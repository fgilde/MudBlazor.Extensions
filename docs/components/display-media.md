# Display and Media Components

Components for displaying various types of content including images, code, markdown, and more.

## MudExImageViewer

Advanced image viewer component with zoom, pan, and rotation capabilities.

### Basic Usage

```razor
<MudExImageViewer Src="@imageUrl" />
```

### Features

- Zoom in/out with mouse wheel or buttons
- Pan/drag to move image
- Rotate image
- Fit to screen modes
- Fullscreen view
- Image metadata display
- Multiple image gallery support
- Thumbnail navigation

### Example

```razor
<MudExImageViewer Src="@imageUrl"
                  Alt="Sample Image"
                  AllowZoom="true"
                  AllowPan="true"
                  AllowRotate="true"
                  ShowToolbar="true"
                  FitMode="ImageFitMode.Contain" />
```

### Gallery Example

```razor
@code {
    private List<string> images = new()
    {
        "image1.jpg",
        "image2.jpg",
        "image3.jpg"
    };
}

<MudExImageViewer Images="@images" 
                  ShowThumbnails="true"
                  AllowSlideshow="true" />
```

## MudExCodeView

Code display component with syntax highlighting and features.

### Basic Usage

```razor
<MudExCodeView Code="@codeContent" Language="csharp" />
```

### Features

- Syntax highlighting for multiple languages
- Line numbers
- Copy to clipboard button
- Line highlighting
- Code folding
- Theme support (light/dark)
- Export options

### Example

```razor
@code {
    private string codeContent = @"
public class HelloWorld
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";
}

<MudExCodeView Code="@codeContent" 
               Language="csharp"
               ShowLineNumbers="true"
               AllowCopy="true"
               Theme="CodeTheme.VisualStudio" />
```

## MudExMarkdown

Markdown rendering component with GitHub-flavored markdown support.

### Basic Usage

```razor
<MudExMarkdown Content="@markdownContent" />
```

### Features

- GitHub-flavored markdown support
- Syntax highlighting for code blocks
- Table support
- Task lists
- Emoji support
- Link handling
- Custom CSS styling
- Safe HTML rendering

### Example

```razor
@code {
    private string markdownContent = @"
# Hello World

This is **bold** and this is *italic*.

## Code Example

```csharp
var greeting = ""Hello, World!"";
Console.WriteLine(greeting);
```

## List
- Item 1
- Item 2
- Item 3
";
}

<MudExMarkdown Content="@markdownContent" 
               EnableSyntaxHighlighting="true"
               EnableTables="true"
               EnableTaskLists="true" />
```

## MudExGravatar

Display Gravatar images based on email addresses.

### Basic Usage

```razor
<MudExGravatar Email="user@example.com" />
```

### Features

- Automatic Gravatar lookup
- Size configuration
- Default image options
- Rating filter
- Rounded or square display
- Fallback support

### Example

```razor
<MudExGravatar Email="user@example.com" 
               Size="80"
               DefaultImage="GravatarDefaultImage.MysteryPerson"
               Rounded="true" />
```

## MudExGravatarCard

Display user information card with Gravatar integration.

### Basic Usage

```razor
<MudExGravatarCard Email="user@example.com" Name="John Doe" />
```

### Features

- Gravatar image display
- User name and email
- Custom content sections
- Action buttons
- Social links
- Responsive design

### Example

```razor
<MudExGravatarCard Email="user@example.com" 
                   Name="John Doe"
                   Title="Software Developer"
                   Bio="Passionate about building great software">
    <ActionContent>
        <MudButton Color="Color.Primary">Follow</MudButton>
        <MudButton Color="Color.Secondary">Message</MudButton>
    </ActionContent>
</MudExGravatarCard>
```

[View Live Demo](https://www.mudex.org/gravatar-card)

## MudExGradientText

Text component with gradient color effects.

### Basic Usage

```razor
<MudExGradientText Text="Gradient Text" />
```

### Features

- Linear and radial gradients
- Multiple color stops
- Custom gradient angles
- Animation support
- Typography integration

### Example

```razor
<MudExGradientText Text="Beautiful Gradient" 
                   GradientColors="@(new[] { "#FF6B6B", "#4ECDC4", "#45B7D1" })"
                   GradientAngle="45"
                   Typo="Typo.h2" />
```

## MudExAudioPlayer

Audio playback component with controls.

### Basic Usage

```razor
<MudExAudioPlayer Source="@audioUrl" />
```

### Features

- Play/pause controls
- Volume control
- Progress bar
- Time display
- Playlist support
- Loop and shuffle
- Playback speed control

### Example

```razor
<MudExAudioPlayer Source="@audioUrl"
                  AutoPlay="false"
                  ShowControls="true"
                  ShowVolume="true"
                  Loop="false" />
```

## MudExIcon

Enhanced icon component with additional features.

### Basic Usage

```razor
<MudExIcon Icon="@Icons.Material.Filled.Home" />
```

### Features

- All MudBlazor icons support
- Custom SVG icons
- Size and color customization
- Animation effects
- Tooltip integration

### Example

```razor
<MudExIcon Icon="@Icons.Material.Filled.Star" 
           Color="Color.Warning"
           Size="Size.Large"
           Title="Favorite" />
```

## Best Practices

- Use `MudExImageViewer` for images requiring user interaction
- Use `MudExCodeView` for displaying code snippets with syntax highlighting
- Use `MudExMarkdown` for rich text content from external sources
- Optimize image sizes before loading
- Consider lazy loading for galleries with many images
- Sanitize user-provided markdown content

## See Also

- [File Display](file-display.md)
- [Other Components](other-components.md)
- [SVG Components](svg.md)
