const LEAFLET_VERSION = '1.9.4';
const LEAFLET_BASE = 'https://cdn.jsdelivr.net/npm/leaflet@' + LEAFLET_VERSION + '/dist/';

class MudExFileDisplayMap {
    constructor(elementRef, dotNet, containerId) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.containerId = containerId;
    }

    // Leaflet is loaded here rather than from C#: the loader resolves when the script has actually executed,
    // so window.L is guaranteed to exist before the first line of map code runs.
    async renderGeoJson(geoJsonString) {
        try {
            const container = document.getElementById(this.containerId);
            if (!container) {
                this.dotnet.invokeMethodAsync('OnError', 'Container element not found');
                return;
            }

            await MudExScriptLoader.loadStyle(LEAFLET_BASE + 'leaflet.css');
            await MudExScriptLoader.require([LEAFLET_BASE + 'leaflet.js'], ['L']);

            const data = JSON.parse(geoJsonString);
            this.setupMap(container);

            if (this.layer) {
                this.layer.remove();
            }

            this.layer = L.geoJSON(data, {
                style: () => ({ color: '#1976d2', weight: 4, opacity: 0.85 }),
                pointToLayer: (feature, latlng) => L.circleMarker(latlng, {
                    radius: 6,
                    color: '#1976d2',
                    fillColor: '#1976d2',
                    fillOpacity: 0.7,
                    weight: 2
                }),
                onEachFeature: (feature, layer) => {
                    const popup = this.popupFor(feature);
                    if (popup) {
                        layer.bindPopup(popup);
                    }
                }
            }).addTo(this.map);

            const bounds = this.layer.getBounds();
            if (bounds && bounds.isValid()) {
                this.map.fitBounds(bounds, { padding: [24, 24] });
            }

            // The file display often mounts the map into a container that gets its size a moment later
            setTimeout(() => this.map.invalidateSize(), 50);

            this.dotnet.invokeMethodAsync('OnMapRendered');
        } catch (e) {
            console.error('Failed to render map:', e);
            this.dotnet.invokeMethodAsync('OnError', e.message || 'Failed to render map');
        }
    }

    popupFor(feature) {
        const properties = (feature && feature.properties) || {};
        const parts = [];
        if (properties.name) {
            parts.push('<strong>' + this.escapeHtml(properties.name) + '</strong>');
        }
        if (properties.description) {
            parts.push(this.escapeHtml(properties.description));
        }
        return parts.length ? parts.join('<br>') : null;
    }

    escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = String(value);
        return div.innerHTML;
    }

    setupMap(container) {
        if (this.map) {
            return;
        }

        this.map = L.map(container, { preferCanvas: true }).setView([0, 0], 2);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        this.resizeObserver = new ResizeObserver(() => this.map && this.map.invalidateSize());
        this.resizeObserver.observe(container);
    }

    dispose() {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }
        if (this.map) {
            this.map.remove();
        }
        this.map = null;
        this.layer = null;
    }
}

window.MudExFileDisplayMap = MudExFileDisplayMap;

export function initializeMudExFileDisplayMap(elementRef, dotnet, containerId) {
    return new MudExFileDisplayMap(elementRef, dotnet, containerId);
}
