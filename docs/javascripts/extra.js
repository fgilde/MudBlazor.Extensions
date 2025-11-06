// Extra JavaScript for MudBlazor.Extensions documentation

document.addEventListener('DOMContentLoaded', function() {
    // Add external link icons
    const links = document.querySelectorAll('a[href^="http"]');
    links.forEach(link => {
        if (!link.hostname.includes('mudblazor.extensions') && 
            !link.hostname.includes('github.io')) {
            link.setAttribute('target', '_blank');
            link.setAttribute('rel', 'noopener noreferrer');
        }
    });
});
