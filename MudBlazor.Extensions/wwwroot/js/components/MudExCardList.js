class MudExCardList {
    _cardCls = ['mud-ex-card', 'mud-ex-animate-all-properties'];

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.options = options;
        this._raf = null;
        this._lastEvent = null;
        this._observer = null;
        this._mutationObserver = null;
        this._visibleCards = new Set();
        this._boundOnPointerMove = this._onPointerMove.bind(this);
        this._boundOnPointerOut = this._onPointerOut.bind(this);
        this.initialize(options);
    }

    _cardIsRelevant(card) {
        return card.getAttribute('data-intersecting') !== 'false';
    }

    _addItemClsIf(el, cls) {
        if (!el) return;
        if (el.classList.contains('mud-ex-card-parent')) {
            this._addItemClsIf(el.querySelector('*'), cls);
        }
        if (el.classList.contains('skip-card-check')) return;

        const arr = Array.from(cls)
            .flatMap(c => c.split(' '))
            .filter(s => s);
        arr.forEach(c => {
            if (!el.classList.contains(c)) el.classList.add(c);
        });
    }

    initialize(options) {
        if (!this.elementRef) return;
        this.options = options;
        this._container = this.elementRef.querySelector('.mud-ex-cards');
        if (!this._container) return;

        // 1) Initial class setup
        const initialCards = Array.from(this._container.children);
        initialCards.forEach(card => {
            this._addItemClsIf(card, this._cardCls);
        });

        // 2) IntersectionObserver for visibility tracking
        this._observer = new IntersectionObserver(entries => {
            entries.forEach(ent => {
                ent.target.dataset.intersecting = ent.isIntersecting;
                if (ent.isIntersecting) {
                    this._visibleCards.add(ent.target);
                } else {
                    this._visibleCards.delete(ent.target);
                }
            });
        }, { threshold: 0.1 });
        initialCards.forEach(card => this._observer.observe(card));

        // 3) Watch for dynamically added cards
        this._mutationObserver = new MutationObserver(mutations => {
            mutations.forEach(m => {
                m.addedNodes.forEach(node => {
                    if (node.nodeType === 1 && node.classList.contains('mud-ex-card')) {
                        this._addItemClsIf(node, this._cardCls);
                        this._observer.observe(node);
                    }
                });
            });
        });
        this._mutationObserver.observe(this._container, { childList: true });

        // 4) Delegate pointer events and throttle via rAF
        this._container.addEventListener('pointermove', this._boundOnPointerMove);
        this._container.addEventListener('pointerout', this._boundOnPointerOut);
    }

    _onPointerMove(e) {
        if (this._raf) cancelAnimationFrame(this._raf);
        this._lastEvent = e;
        this._raf = requestAnimationFrame(() => {
            this._handlePointerMove(this._lastEvent);
        });
    }

    _handlePointerMove(e) {
        // Update CSS vars on all visible cards for the lightbulb effect
        this._visibleCards.forEach(card => {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            card.style.setProperty('--mouse-x', `${x}px`);
            card.style.setProperty('--mouse-y', `${y}px`);
        });

        // 3D effect only on the hovered card
        if (this.options && this.options.use3dEffect) {
            const card = e.target.closest('.mud-ex-card');
            if (card && this._cardIsRelevant(card)) {
                this.cardMouseMove(card, e);
            }
        }
    }

    _onPointerOut(e) {
        // Reset transforms when pointer leaves a card
        const card = e.target.closest('.mud-ex-card');
        if (!card) return;
        card.style.boxShadow = '';
        card.style.transform = '';
    }

    cardMouseMove(card, e) {
        if (!(this.options && this.options.use3dEffect)) return;
        const rect = card.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        const width = rect.width;
        const height = rect.height;
        const centerX = x - width / 2;
        const centerY = y - height / 2;
        const d = Math.sqrt(centerX ** 2 + centerY ** 2);

        const boxShadow = `${-centerX / 5}px ${-centerY / 10}px 10px rgba(0, 0, 0, 0.2)`;
        const transform = `rotate3d(${-centerY / 100}, ${centerX / 100}, 0, ${d / 8}deg)`;
        card.style.boxShadow = boxShadow;
        card.style.transform = transform;
    }

    dispose() {
        // Remove event listeners
        if (this._container) {
            this._container.removeEventListener('pointermove', this._boundOnPointerMove);
            this._container.removeEventListener('pointerout', this._boundOnPointerOut);
        }
        // Disconnect observers
        if (this._observer) this._observer.disconnect();
        if (this._mutationObserver) this._mutationObserver.disconnect();
        // Cancel any pending frame
        if (this._raf) cancelAnimationFrame(this._raf);
    }
}

window.MudExCardList = MudExCardList;

export function initializeMudExCardList(elementRef, dotnet, options) {
    return new MudExCardList(elementRef, dotnet, options);
}
