class MudExCardList {

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.initialize(options);
    }


    initialize(options) {
        var me = this;
        this.options = options;
        if (!this.elementRef) {
            return;
        }
        this._container = this.elementRef.querySelector(".mud-ex-cards");        
        this._container.onmousemove = null;
        for (const card of Array.from(this._container.children)) {
            card._isMouseOver = false;
        }
        
        this._container.onmousemove = e => {
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

                if (card._isMouseOver)
                    continue;
                                    
                card.addEventListener('mouseenter', () => {                    
                    card.addEventListener('mousemove', this.cardMouseMove.bind(this, card));
                });

                card.addEventListener('mouseleave', () => {
                    card.removeEventListener('mousemove', this.cardMouseMove.bind(this, card));
                    card.style.removeProperty('--transform');
                    card.style.removeProperty('--shadow');
                    card.style.boxShadow = '';
                    card.style.transform = '';
                });
                card._isMouseOver = true;
            };
        };

    }

    cardMouseMove(card, e) {
        var options = this.options;
        if (options && options.use3dEffect) {
            const rect = card.getBoundingClientRect(),
                x = e.clientX - rect.left,
                y = e.clientY - rect.top,
                width = rect.width,
                height = rect.height,
                centerX = x - width / 2,
                centerY = y - height / 2,
                d = Math.sqrt(centerX ** 2 + centerY ** 2);


            let boxShadow = `${-centerX / 5}px ${-centerY / 10}px 10px rgba(0, 0, 0, 0.2)`;
            let transform = `rotate3d(${-centerY / 100}, ${centerX / 100}, 0, ${d / 8}deg)`;
            card.style.boxShadow = boxShadow;
            card.style.transform = transform;
            //card.style.setProperty('--shadow', boxShadow);
            //card.style.setProperty('--transform', transform);
        }
    }

    dispose() {
        this._container.onmousemove = null;

        if (this.options && this.options.use3dEffect) {
            for (const card of Array.from(this._container.children)) {
                card.removeEventListener('mouseenter', this.cardMouseMove.bind(this, card));
                card.removeEventListener('mouseleave', this.cardMouseMove.bind(this, card));
                card.style.boxShadow = '';
                card.style.transform = '';
            }
        }
    }
}


window.MudExCardList = MudExCardList;

export function initializeMudExCardList(elementRef, dotnet, options) {    
    return new MudExCardList(elementRef, dotnet, options);
}
