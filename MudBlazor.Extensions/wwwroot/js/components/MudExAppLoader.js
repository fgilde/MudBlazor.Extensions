class MudExAppLoader extends HTMLElement {

    css = `
       :root{--blazor-load-percentage:0;--loading-color:#ff4081;}.mud-ex-app-loader-loading-container{display:flex;align-items:center;justify-content:center;height:100vh;width:100vw;position:fixed;z-index:99999;background-color:rgba(0,0,0,.9);}.mud-ex-app-loader-loading-container .mud-ex-app-loader-loading-progress{display:flex;align-items:center;justify-content:center;}.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle{transform:rotate(-90deg);}.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle-bg,.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle-progress{fill:none;stroke-width:10;}.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle-bg{stroke:#e6e6e6;}.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle-progress{stroke:var(--loading-color);stroke-dasharray:566;stroke-dashoffset:calc(566 - var(--blazor-load-percentage)*5.66);}.mud-ex-app-loader-loading-container .logo{position:absolute;max-width:var(--size);}.mud-ex-app-loader-loading-container .mud-ex-app-loader-loading-percentage{position:absolute;margin-top:250px;font-size:1.5rem;color:var(--loading-color);}.mud-ex-app-loader-loading-container.complete .loading-percentage{display:none;}.mud-ex-app-loader-loading-container .mud-ex-app-loader-loading-percentage.hidden{display:none;}.mud-ex-app-loader-loading-container.pixelated-out{animation:pixelatedOut 1s forwards;}.mud-ex-app-loader-loading-container .mud-ex-app-loader-circle-glowing{animation:glowing 1.5s infinite;stroke-opacity:0;border:14px solid var(--loading-color);border-radius:50%;}.mud-ex-app-loader-loading-container .app-name{position:absolute;margin-top:355px;font-size:2rem;font-weight:bold;color:var(--app-name-text-color);}@keyframes pixelatedOut{0%{opacity:1;}100%{opacity:0;transform:scale(3.5);}}@keyframes glowing{0%,100%{box-shadow:0 0 5px var(--loading-color),0 0 10px var(--loading-color),0 0 20px var(--loading-color),0 0 30px var(--loading-color);}50%{box-shadow:0 0 10px var(--loading-color),0 0 20px var(--loading-color),0 0 30px var(--loading-color),0 0 40px var(--loading-color);}}
    `;

    constructor() {
        super();
        // this.loadStylesheet('_content/MudBlazor.Extensions/css/MudExAppLoader.min.css');
        this.loadInlineStyles(this.css);
    }

    loadInlineStyles(css) {
        try {
            const style = document.createElement('style');
            style.type = 'text/css';
            style.appendChild(document.createTextNode(css));
            document.head.appendChild(style);
        } catch (e) {
            // Handle the error
        }
    }

    loadStylesheet(href) {
        try {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onerror = () => {
                this.removeLoadingContainer();
            };
            document.head.appendChild(link);
        } catch (e) {

        }
    }

    connectedCallback() {
        this.setAttributes();
        this.addPercentageObserver();
        this.addContentChangeObserver();
        this.setNewMarkup();
    }

    setAttributes() {
        this.accentColor = this.getAttribute('AccentColor') || '#ff0000';
        this.loadingTextColor = this.getAttribute('LoadingTextColor') || this.accentColor;
        this.appNameColor = this.getAttribute('AppNameColor') || this.accentColor;

        this.mainAppId = this.getAttribute('MainAppId') || this.getAttribute('AppId') || 'app';
        this.logo = this.getAttribute('Logo') || '';
        this.timeout = parseInt(this.getAttribute('Timeout')) || 2000;
        this.preLoadText = this.getAttribute('PreLoadText') || 'Loading...';
        this.appName = this.getAttribute('AppName') || '';
        this.subTitle = this.getAttribute('SubTitle') || '';
        this.subTitleClass = this.getAttribute('subTitleClass') || '';
        this.size = this.getAttribute('Size') || '200';

        // Set CSS variable --loading-color to accentColor value
        document.documentElement.style.setProperty('--loading-color', this.accentColor);
        document.documentElement.style.setProperty('--loading-text-color', this.loadingTextColor);
        document.documentElement.style.setProperty('--app-name-text-color', this.appNameColor);
        document.documentElement.style.setProperty('--size', this.size + 'px');

        // Add attributes for additional classes
        this.containerClass = this.getAttribute('ContainerClass') || '';
        this.progressClass = this.getAttribute('ProgressClass') || '';
        this.circleClass = this.getAttribute('CircleClass') || '';
        this.logoClass = this.getAttribute('LogoClass') || '';
        this.percentageClass = this.getAttribute('PercentageClass') || '';
        this.appNameClass = this.getAttribute('AppNameClass') || '';

        // Add attribute for the callback function
        this.onAppLoaded = this.getAttribute('OnAppLoaded') || '';
        this.onAnimationFinished = this.getAttribute('OnAnimationFinished') || '';

    }

    setNewMarkup() {
        if (!this.logo && !this.innerHTML) {
            return setTimeout(() => this.setNewMarkup(), 100);
        }

        const newMarkup = `
          <div class="mud-ex-app-loader-loading-container ${this.containerClass}">
            <div class="mud-ex-app-loader-loading-progress ${this.progressClass}">
              <svg class="mud-ex-app-loader-circle ${this.circleClass}" width="${this.size}" height="${this.size}" viewBox="0 0 ${this.size} ${this.size}">
                <circle class="mud-ex-app-loader-circle-bg" cx="100" cy="100" r="90" />
                <circle class="mud-ex-app-loader-circle-progress" cx="100" cy="100" r="90" style="stroke: ${this.accentColor}" />
              </svg>
              ${this.logo ? `<img class="logo ${this.logoClass}" src="${this.logo}" />` : `<div class="logo ${this.logoClass}">${this.innerHTML}</div>`}
              <span class="mud-ex-app-loader-loading-percentage ${this.percentageClass}">${this.preLoadText}</span>
              <div class="app-name ${this.appNameClass}">
                <span>${this.appName}</span> <br />
                <center><span class="${this.subTitleClass}">${this.subTitle}</span></center>
              </div>
                           
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

    addContentChangeObserver() {
        document.addEventListener('DOMContentLoaded', () => {
            const appElement = document.getElementById(this.mainAppId);
            if (appElement) {
                this.observeContentChanges(appElement, (observer) => {
                    const loadingContainer = this.querySelector('.mud-ex-app-loader-loading-container');
                    loadingContainer.classList.add('pixelated-out');
                    observer.disconnect(); // Stop observing changes
                    this.removeLoadingContainer(this.timeout || 2000);
                });
            }
            else {
                this.removeLoadingContainer(this.timeout || 2000);
            }            
        });
    }

    raiseLoaded() {
        if (!this._loadedRaised) {
            this._loadedRaised = true;
            this.raise(this.onAppLoaded);
        }
    }

    raiseAnimationFinished() {
        this.raise(this.onAnimationFinished);
    }

    raise(evt) {
        if (typeof window[evt] === 'function') {
            window[evt]();
        }
        else if (typeof evt === 'function') {
            evt();
        }
        else if (typeof evt === 'string') {
            eval(evt);
        }
    }

    dispose() {
        if (!this.disposing) {
            this.raiseAnimationFinished();
            this.disposing = this.disposed = true;
            if (!this.querySelector('#' + this.mainAppId)) {
                this.remove();   
            }            
        }
    }

    removeLoadingContainer(timeout) {
        this.raiseLoaded();
        setTimeout(() => {
            const loadingContainer = this.querySelector('.mud-ex-app-loader-loading-container');
            if (loadingContainer) {
                loadingContainer.remove();
            }            
            this.dispose();
        }, timeout || 1);
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

    updateUI(percentage) {
        var el = this.querySelector('.mud-ex-app-loader-loading-percentage');
        if (el) {
            el.innerHTML = `${percentage}%`;
        }
        this.updateCircleProgress(percentage);

        if (percentage >= 100) {
            setTimeout(() => {
                var circleLoader = this.querySelector('.mud-ex-app-loader-circle');
                if (circleLoader) {
                    circleLoader.classList.add('mud-ex-app-loader-circle-glowing');
                }
            }, 2);
        }
    }

    updateCircleProgress(percentage) {
        const circleProgress = this.querySelector('.mud-ex-app-loader-circle-progress');
        if (circleProgress) {
            const radius = circleProgress.getAttribute('r');
            const circumference = 2 * Math.PI * radius;

            const progress = percentage / 100;
            const dashOffset = circumference * (1 - progress);

            circleProgress.style.strokeDasharray = `${circumference} ${circumference}`;
            circleProgress.style.strokeDashoffset = dashOffset;
        }
    }
}

customElements.define('mud-ex-app-loader', MudExAppLoader);