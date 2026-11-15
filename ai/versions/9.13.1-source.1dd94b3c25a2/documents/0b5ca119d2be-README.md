# MudBlazor.Extensions Documentation

This directory contains the source files for the MudBlazor.Extensions documentation site built with MkDocs and the Material theme.

## Building Documentation Locally

### Prerequisites

- Python 3.x
- pip

### Installation

Install the required dependencies:

```bash
pip install mkdocs mkdocs-material mkdocs-material-extensions pymdown-extensions
```

### Build and Serve

To build and serve the documentation locally:

```bash
mkdocs serve
```

The documentation will be available at `http://localhost:8000`

### Build Only

To build the documentation without serving:

```bash
mkdocs build
```

The built site will be in the `site/` directory.

## Deployment

The documentation is automatically deployed to GitHub Pages when changes are pushed to the `main` branch. The deployment is handled by the GitHub Actions workflow at `.github/workflows/docs.yml`.

## Documentation Structure

- `docs/` - Documentation source files
  - `index.md` - Homepage
  - `getting-started/` - Installation and setup guides
  - `components/` - Component documentation
  - `extensions/` - Extension method documentation
  - `utilities/` - Utility class documentation
  - `services/` - Service documentation
  - `api/` - API reference
  - `about/` - License and contributing information
  - `stylesheets/` - Custom CSS
  - `javascripts/` - Custom JavaScript

- `mkdocs.yml` - MkDocs configuration file

## Theme Customization

The documentation uses the Material theme with custom colors matching the MudBlazor.Extensions sample page:

- Primary color: `#199b90`
- Appbar background: `#1f2226`
- Success color: `#19635d`

Custom styles are defined in `docs/stylesheets/extra.css`.

## Contributing to Documentation

1. Edit or create markdown files in the `docs/` directory
2. Test your changes locally with `mkdocs serve`
3. Submit a pull request with your changes
4. The documentation will be automatically deployed after merge

## Need Help?

- [MkDocs Documentation](https://www.mkdocs.org/)
- [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/)
- [MudBlazor.Extensions Repository](https://github.com/fgilde/MudBlazor.Extensions)
