function observeContentChanges(element, callback) {
    const observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => {
            if (mutation.type === 'childList') {
                callback(observer);
            }
        });
    });

    observer.observe(element, { childList: true });
}

function observeCSSVariable(variableName, callback) {
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

function updateCircleProgress(percentage) {
    const circle = document.querySelector('.circle-progress');
    const radius = circle.getAttribute('r');
    const circumference = 2 * Math.PI * radius;

    const progress = percentage / 100;
    const dashOffset = circumference * (1 - progress);

    circle.style.strokeDasharray = `${circumference} ${circumference}`;
    circle.style.strokeDashoffset = dashOffset;
}

let prevPercentage = -1;

function updateUI(percentage) {
    document.querySelector('.loading-percentage').innerHTML = `${percentage}%`;
    updateCircleProgress(percentage);

    if (percentage >= 100) {
        setTimeout(() => {
            document.querySelector('.circle').classList.add('circle-glowing');

            const appElement = document.getElementById('app');
            observeContentChanges(appElement, (observer) => {
                const loadingContainer = document.querySelector('.loading-container');
                loadingContainer.classList.add('pixelated-out');
                observer.disconnect(); // Stop observing changes

                // Remove the loading container after the animation is complete
                setTimeout(() => {
                    loadingContainer.remove();
                }, 1000); // This should match the duration of the pixelatedOut animation
            });

        }, 2);
    }
}

function onPercentageChanged(value, observer) {
    const percentage = Math.round(value);
    if (percentage !== prevPercentage) {
        prevPercentage = percentage;
        requestAnimationFrame(() => updateUI(percentage));
    }

    if (value >= 100) {
        observer.disconnect(); // Stop observing changes
    }
}

observeCSSVariable('--blazor-load-percentage', onPercentageChanged);
