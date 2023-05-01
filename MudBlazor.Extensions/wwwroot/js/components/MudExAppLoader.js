class MudExAppLoader extends HTMLElement {
    constructor() {
        super();
        this.loadStylesheet('_content/MudBlazor.Extensions/css/MudExAppLoader.css');
    }

    loadStylesheet(href) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onerror = () => {
            const loadingContainer = this.querySelector('.mud-ex-app-loader-loading-container');
            if (loadingContainer) {
                loadingContainer.remove();
            }
        };
        document.head.appendChild(link);
    }

    connectedCallback() {
        this.setAttributes();
        this.setNewMarkup();
        this.addPercentageObserver();
    }

    setAttributes() {
        this.accentColor = this.getAttribute('AccentColor') || '#ff0000';
        this.mainAppId = this.getAttribute('MainAppId') || 'app';
        this.logo = this.getAttribute('Logo') || '';

        // Set CSS variable --loading-color to accentColor value
        document.documentElement.style.setProperty('--loading-color', this.accentColor);
    }

    setNewMarkup() {
        const newMarkup = `
          <div class="mud-ex-app-loader-loading-container">
            <div class="mud-ex-app-loader-loading-progress">
              <svg class="mud-ex-app-loader-circle" width="200" height="200" viewBox="0 0 200 200">
                <circle class="mud-ex-app-loader-circle-bg" cx="100" cy="100" r="90" />
                <circle class="mud-ex-app-loader-circle-progress" cx="100" cy="100" r="90" style="stroke: ${this.accentColor}" />
              </svg>
                <img class="logo" src="${this.logo}" /> 
              <span class="mud-ex-app-loader-loading-percentage">Loading...</span>
            </div>
          </div>
        `;
        this.innerHTML = newMarkup;
    }

    addPercentageObserver() {
        const onPercentageChanged = (value, observer) => {
            const percentage = Math.round(value);
            if (percentage !== this.prevPercentage) {
                this.prevPercentage = percentage;
                requestAnimationFrame(() => this.updateUI(percentage));
            }

            if (value >= 100) {
                observer.disconnect(); // Stop observing changes
            }
        };

        this.observeCSSVariable('--blazor-load-percentage', onPercentageChanged);
    }

    observeCSSVariable(variableName, callback) {
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                if (mutation.attributeName === 'style') {
                    const newValue = getComputedStyle(mutation.target).getPropertyValue(variableName).trim();
                    if (newValue) {
                        const parsedValue = parseFloat(newValue);
                        callback(parsedValue, observer);
                    }
                }
            });
        });

        observer.observe(document.documentElement, { attributes: true });
    }

    updateUI(percentage) {
        this.querySelector('.mud-ex-app-loader-loading-percentage').innerHTML = `${percentage}%`;
        this.updateCircleProgress(percentage);

        if (percentage >= 100) {
            setTimeout(() => {
                this.querySelector('.mud-ex-app-loader-circle').classList.add('mud-ex-app-loader-circle-glowing');
                this.addContentChangeObserver();
            }, 2);
        }
    }

    addContentChangeObserver() {
        const appElement = document.getElementById(this.mainAppId);
        this.observeContentChanges(appElement, (observer) => {
            const loadingContainer = this.querySelector('.mud-ex-app-loader-loading-container');
            loadingContainer.classList.add('pixelated-out');
            observer.disconnect(); // Stop observing changes

            // Remove the loading container after the animation is complete
            setTimeout(() => {
                loadingContainer.remove();

            }, 1000); // This should match the duration of the pixelatedOut animation
        });
    }

    observeContentChanges(element, callback) {
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                if (mutation.type === 'childList') {
                    callback(observer);
                }
            });
        });

        observer.observe(element, { childList: true });
    }

    updateCircleProgress(percentage) {
        const circle = this.querySelector('.mud-ex-app-loader-circle-progress');
        const radius = circle.getAttribute('r');
        const circumference = 2 * Math.PI * radius;

        const progress = percentage / 100;
        const dashOffset = circumference * (1 - progress);

        circle.style.strokeDasharray = `${circumference} ${circumference}`;
        circle.style.strokeDashoffset = dashOffset;
    }
}

customElements.define('mud-ex-app-loader', MudExAppLoader);