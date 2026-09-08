// Pinned deliberately: three.js removed the UMD builds under examples/js in r148 and ships the loaders as ES
// modules with bare "three" imports only. Bumping this past 0.147.x breaks every loader listed below.
const THREE_VERSION = '0.137.5';
const THREE_BASE = 'https://cdn.jsdelivr.net/npm/three@' + THREE_VERSION + '/';
const FFLATE_URL = 'https://cdn.jsdelivr.net/npm/fflate@0.8.2/umd/index.js';

class MudExFileDisplayModel3d {
    constructor(elementRef, dotNet, containerId) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.containerId = containerId;
        this.disposed = false;
    }

    // three.js is loaded here rather than from C#: the loader resolves when a script has actually executed, and
    // the addon loaders have to come strictly after the base library that they extend.
    async ensureThree() {
        await MudExScriptLoader.require([THREE_BASE + 'build/three.min.js'], ['THREE']);
        await MudExScriptLoader.loadScript(FFLATE_URL); // 3MFLoader unzips the container with fflate
        await MudExScriptLoader.require([
            THREE_BASE + 'examples/js/controls/OrbitControls.js',
            THREE_BASE + 'examples/js/loaders/STLLoader.js',
            THREE_BASE + 'examples/js/loaders/OBJLoader.js',
            THREE_BASE + 'examples/js/loaders/PLYLoader.js',
            THREE_BASE + 'examples/js/loaders/GLTFLoader.js',
            THREE_BASE + 'examples/js/loaders/3MFLoader.js'
        ], ['THREE.OrbitControls', 'THREE.STLLoader', 'THREE.GLTFLoader']);
    }

    async renderModel(fileBytes, format, wireframe, autoRotate) {
        try {
            const container = document.getElementById(this.containerId);
            if (!container) {
                this.dotnet.invokeMethodAsync('OnError', 'Container element not found');
                return;
            }

            await this.ensureThree();
            this.setupScene(container);
            this.autoRotate = !!autoRotate;
            this.wireframe = !!wireframe;

            const buffer = new Uint8Array(fileBytes).buffer;
            this.loadFormat(buffer, (format || '').toLowerCase());
        } catch (e) {
            console.error('Failed to render 3d model:', e);
            this.dotnet.invokeMethodAsync('OnError', e.message || 'Failed to render 3d model');
        }
    }

    loadFormat(buffer, format) {
        const self = this;
        switch (format) {
            case 'stl':
                this.showObject(this.meshFromGeometry(new THREE.STLLoader().parse(buffer)));
                break;
            case 'ply':
                this.showObject(this.meshFromGeometry(new THREE.PLYLoader().parse(buffer)));
                break;
            case 'obj':
                this.showObject(new THREE.OBJLoader().parse(new TextDecoder().decode(buffer)));
                break;
            case '3mf':
                this.showObject(new THREE.ThreeMFLoader().parse(buffer));
                break;
            case 'gltf':
            case 'glb':
                // GLTFLoader is the only one of these that works asynchronously
                new THREE.GLTFLoader().parse(buffer, '',
                    gltf => self.showObject(gltf.scene),
                    error => self.dotnet.invokeMethodAsync('OnError', (error && error.message) || 'Failed to parse glTF'));
                break;
            default:
                this.dotnet.invokeMethodAsync('OnError', 'Unsupported 3d format: ' + format);
        }
    }

    meshFromGeometry(geometry) {
        if (!geometry.attributes.normal) {
            geometry.computeVertexNormals();
        }
        const material = new THREE.MeshStandardMaterial({
            color: 0xb0b6bd,
            metalness: 0.15,
            roughness: 0.65,
            flatShading: !geometry.attributes.normal,
            vertexColors: !!geometry.attributes.color
        });
        return new THREE.Mesh(geometry, material);
    }

    setupScene(container) {
        if (this.renderer) {
            return;
        }

        this.scene = new THREE.Scene();

        this.camera = new THREE.PerspectiveCamera(45, this.aspect(container), 0.01, 100000);
        this.camera.position.set(0, 0, 10);

        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(window.devicePixelRatio || 1);
        this.renderer.setClearColor(0x000000, 0);
        this.renderer.setSize(container.clientWidth || 1, container.clientHeight || 1);
        container.appendChild(this.renderer.domElement);

        this.scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.1));
        const directional = new THREE.DirectionalLight(0xffffff, 0.85);
        directional.position.set(1, 1, 1);
        this.scene.add(directional);

        this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.08;

        // The file display can be resized (dialog, splitter, ...) without the window ever firing a resize event
        this.resizeObserver = new ResizeObserver(() => this.resize(container));
        this.resizeObserver.observe(container);

        const animate = () => {
            if (this.disposed) {
                return;
            }
            this.animationHandle = requestAnimationFrame(animate);
            if (this.autoRotate && this.model) {
                this.model.rotation.y += 0.006;
            }
            this.controls.update();
            this.renderer.render(this.scene, this.camera);
        };
        animate();
    }

    showObject(object) {
        if (this.model) {
            this.scene.remove(this.model);
        }
        this.model = object;
        this.scene.add(object);

        this.applyWireframe(this.wireframe);
        this.fitCamera(object);
        this.grid = this.addGrid(object);

        this.dotnet.invokeMethodAsync('OnModelLoaded', this.countTriangles(object));
    }

    // Centers the object on the origin and moves the camera far enough back to see all of it
    fitCamera(object) {
        object.updateMatrixWorld(true);
        const box = new THREE.Box3().setFromObject(object);
        if (box.isEmpty()) {
            return;
        }

        const center = box.getCenter(new THREE.Vector3());
        object.position.sub(center);

        const size = box.getSize(new THREE.Vector3()).length();
        const distance = size / (2 * Math.tan((this.camera.fov * Math.PI) / 360)) * 1.4;

        this.camera.near = size / 1000;
        this.camera.far = size * 100;
        this.camera.updateProjectionMatrix();

        this.homePosition = new THREE.Vector3(distance * 0.6, distance * 0.5, distance * 0.8);
        this.resetView();
    }

    addGrid(object) {
        if (this.grid) {
            this.scene.remove(this.grid);
        }
        object.updateMatrixWorld(true);
        const box = new THREE.Box3().setFromObject(object);
        const size = box.getSize(new THREE.Vector3()).length() || 10;
        const grid = new THREE.GridHelper(size * 2, 20, 0x888888, 0x888888);
        grid.material.opacity = 0.2;
        grid.material.transparent = true;
        grid.position.y = box.min.y;
        this.scene.add(grid);
        return grid;
    }

    countTriangles(object) {
        let triangles = 0;
        object.traverse(child => {
            const geometry = child.geometry;
            if (!geometry || !geometry.attributes || !geometry.attributes.position) {
                return;
            }
            triangles += geometry.index ? geometry.index.count / 3 : geometry.attributes.position.count / 3;
        });
        return Math.round(triangles);
    }

    applyWireframe(value) {
        if (!this.model) {
            return;
        }
        this.model.traverse(child => {
            if (!child.material) {
                return;
            }
            const materials = Array.isArray(child.material) ? child.material : [child.material];
            materials.forEach(material => { material.wireframe = value; });
        });
    }

    setWireframe(value) {
        this.wireframe = !!value;
        this.applyWireframe(this.wireframe);
    }

    setAutoRotate(value) {
        this.autoRotate = !!value;
    }

    resetView() {
        if (!this.camera || !this.homePosition) {
            return;
        }
        this.camera.position.copy(this.homePosition);
        this.controls.target.set(0, 0, 0);
        this.controls.update();
    }

    aspect(container) {
        const width = container.clientWidth || 1;
        const height = container.clientHeight || 1;
        return width / height;
    }

    resize(container) {
        if (!this.renderer || !container.clientWidth || !container.clientHeight) {
            return;
        }
        this.camera.aspect = this.aspect(container);
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(container.clientWidth, container.clientHeight);
    }

    dispose() {
        this.disposed = true;
        if (this.animationHandle) {
            cancelAnimationFrame(this.animationHandle);
        }
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }
        if (this.controls) {
            this.controls.dispose();
        }
        if (this.renderer) {
            this.renderer.dispose();
            if (this.renderer.domElement && this.renderer.domElement.parentNode) {
                this.renderer.domElement.parentNode.removeChild(this.renderer.domElement);
            }
        }
        this.renderer = null;
        this.scene = null;
        this.model = null;
    }
}

window.MudExFileDisplayModel3d = MudExFileDisplayModel3d;

export function initializeMudExFileDisplayModel3d(elementRef, dotnet, containerId) {
    return new MudExFileDisplayModel3d(elementRef, dotnet, containerId);
}
