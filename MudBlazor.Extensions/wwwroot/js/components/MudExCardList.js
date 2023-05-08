class MudExCardList {

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.initialize(options);
    }


    initialize(options) {
        this.options = options;
        this._container = this.elementRef.querySelector(".mud-ex-cards");
        this._container.onmousemove = e => {
            //const cards = Array.from(cardsParent.children).filter(child => child.classList.contains('mud-ex-card'));
            // const cards = Array.from(this._container.children);
            for (const card of Array.from(this._container.children)) {
                if (!card.classList.contains('mud-ex-card')) {
                    card.classList.add('mud-ex-card');
                    card.classList.add('mud-ex-animate-all-properties');
                }
                const rect = card.getBoundingClientRect(),
                    x = e.clientX - rect.left,
                    y = e.clientY - rect.top;

                card.style.setProperty('--mouse-x', `${x}px`);
                card.style.setProperty('--mouse-y', `${y}px`);
            };
        };
    }


    dispose() {
        this._container.onmousemove = null;
    }
}


window.MudExCardList = MudExCardList;

export function initializeMudExCardList(elementRef, dotnet, options) {
    return new MudExCardList(elementRef, dotnet, options);
}