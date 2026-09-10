class MudExFileSystemAccess {

    // Handles cannot cross the interop boundary, so they stay here and .NET refers to them by id. The id is
    // the path from the picked root ("/", "/docs", "/docs/a.txt"), not an incrementing counter: re-listing a
    // directory the user already visited overwrites the same key instead of minting a new one, so the map
    // stays bounded to the distinct paths of the current tree instead of growing for the page's whole
    // lifetime. Picking a new root clears it outright, so switching folders doesn't pile the old tree on top.
    static _handles = {};

    static isSupported() {
        return typeof window.showDirectoryPicker === 'function';
    }

    /** Opens the picker. Returns the root entry, or null when the user cancelled. */
    static async pickDirectory(writable) {
        if (!MudExFileSystemAccess.isSupported()) {
            return null;
        }
        try {
            const handle = await window.showDirectoryPicker({ mode: writable ? 'readwrite' : 'read' });
            MudExFileSystemAccess._handles = { '/': handle };
            return { id: '/', name: handle.name, writable: await MudExFileSystemAccess.canWrite(handle) };
        } catch (e) {
            // AbortError is the user closing the dialog - not an error worth reporting
            if (e && e.name === 'AbortError') {
                return null;
            }
            throw e;
        }
    }

    static async canWrite(handle) {
        if (!handle.queryPermission) {
            return false;
        }
        return (await handle.queryPermission({ mode: 'readwrite' })) === 'granted';
    }

    static async listDirectory(handleId) {
        const handle = MudExFileSystemAccess._handles[handleId];
        if (!handle) {
            return [];
        }

        const entries = [];
        for await (const [name, child] of handle.entries()) {
            const id = handleId === '/' ? '/' + name : handleId + '/' + name;
            MudExFileSystemAccess._handles[id] = child;

            const isDirectory = child.kind === 'directory';
            let size = 0;
            let lastModified = null;
            let contentType = null;
            if (!isDirectory) {
                const file = await child.getFile();
                size = file.size;
                lastModified = new Date(file.lastModified).toISOString();
                contentType = file.type || null;
            }
            entries.push({ id, name, isDirectory, size, lastModified, contentType });
        }
        return entries;
    }

    /** Returns a blob url for a file handle. The caller revokes it. */
    static async createFileUrl(handleId) {
        const handle = MudExFileSystemAccess._handles[handleId];
        if (!handle || handle.kind !== 'file') {
            return null;
        }
        return URL.createObjectURL(await handle.getFile());
    }

    static revokeUrl(url) {
        URL.revokeObjectURL(url);
    }

    static _join(parentHandleId, name) {
        return parentHandleId === '/' ? '/' + name : parentHandleId + '/' + name;
    }

    // Drops a handle and everything below it from the map. Ids are paths, so a moved or deleted directory
    // takes its whole subtree's keys with it - leaving them would let a later lookup resolve a path that no
    // longer exists.
    static _forget(handleId) {
        const prefix = handleId === '/' ? '/' : handleId + '/';
        Object.keys(MudExFileSystemAccess._handles).forEach(key => {
            if (key === handleId || key.startsWith(prefix)) {
                delete MudExFileSystemAccess._handles[key];
            }
        });
    }

    static async _entry(id, name, handle) {
        MudExFileSystemAccess._handles[id] = handle;
        if (handle.kind === 'directory') {
            return { id, name, isDirectory: true, size: 0, lastModified: null, contentType: null };
        }
        const file = await handle.getFile();
        return {
            id,
            name,
            isDirectory: false,
            size: file.size,
            lastModified: new Date(file.lastModified).toISOString(),
            contentType: file.type || null
        };
    }

    /** True when the directory already holds an entry of that name, whatever kind it is. */
    static async entryExists(parentHandleId, name) {
        const parent = MudExFileSystemAccess._handles[parentHandleId];
        if (!parent || parent.kind !== 'directory') {
            return false;
        }
        try {
            await parent.getFileHandle(name);
            return true;
        } catch (e) {
            // not a file - fall through and try a directory
        }
        try {
            await parent.getDirectoryHandle(name);
            return true;
        } catch (e) {
            return false;
        }
    }

    static async createDirectory(parentHandleId, name) {
        const parent = MudExFileSystemAccess._handles[parentHandleId];
        if (!parent || parent.kind !== 'directory') {
            return null;
        }
        const handle = await parent.getDirectoryHandle(name, { create: true });
        return await MudExFileSystemAccess._entry(MudExFileSystemAccess._join(parentHandleId, name), name, handle);
    }

    /**
     * Writes a file into the directory. The content arrives as a DotNetStreamReference so the bytes are
     * transferred as a binary stream instead of being base64'd through the JSON interop channel.
     */
    static async writeFile(parentHandleId, name, streamRef) {
        const parent = MudExFileSystemAccess._handles[parentHandleId];
        if (!parent || parent.kind !== 'directory') {
            return null;
        }
        const buffer = await streamRef.arrayBuffer();
        const handle = await parent.getFileHandle(name, { create: true });
        const writable = await handle.createWritable();
        await writable.write(buffer);
        await writable.close();
        return await MudExFileSystemAccess._entry(MudExFileSystemAccess._join(parentHandleId, name), name, handle);
    }

    static async remove(handleId) {
        const handle = MudExFileSystemAccess._handles[handleId];
        if (!handle) {
            return false;
        }
        if (typeof handle.remove === 'function') {
            await handle.remove({ recursive: true });
        } else {
            const cut = handleId.lastIndexOf('/');
            const parent = MudExFileSystemAccess._handles[cut <= 0 ? '/' : handleId.substring(0, cut)];
            if (!parent) {
                return false;
            }
            await parent.removeEntry(handle.name, { recursive: true });
        }
        MudExFileSystemAccess._forget(handleId);
        return true;
    }

    /**
     * Moves or renames an entry. Files use the browser's own move(), which is atomic. Directories have no
     * move(), so they are copied and only then removed - a failed copy leaves the original untouched.
     */
    static async moveEntry(handleId, targetDirHandleId, newName) {
        const handle = MudExFileSystemAccess._handles[handleId];
        const target = MudExFileSystemAccess._handles[targetDirHandleId];
        if (!handle || !target || target.kind !== 'directory') {
            return null;
        }

        const name = newName || handle.name;
        if (handle.kind === 'file' && typeof handle.move === 'function') {
            await handle.move(target, name);
            MudExFileSystemAccess._forget(handleId);
        } else {
            await MudExFileSystemAccess._copyInto(handle, target, name);
            await MudExFileSystemAccess.remove(handleId);
        }

        const id = MudExFileSystemAccess._join(targetDirHandleId, name);
        const moved = handle.kind === 'directory'
            ? await target.getDirectoryHandle(name)
            : await target.getFileHandle(name);
        return await MudExFileSystemAccess._entry(id, name, moved);
    }

    static async _copyInto(handle, targetDir, name) {
        if (handle.kind === 'file') {
            const file = await handle.getFile();
            const dest = await targetDir.getFileHandle(name, { create: true });
            const writable = await dest.createWritable();
            await writable.write(file);
            await writable.close();
            return dest;
        }

        const dest = await targetDir.getDirectoryHandle(name, { create: true });
        for await (const [childName, child] of handle.entries()) {
            await MudExFileSystemAccess._copyInto(child, dest, childName);
        }
        return dest;
    }

    /** Relative paths of the files an <input webkitdirectory> collected, in the order of input.files. */
    static relativePaths(inputElement) {
        const files = inputElement && inputElement.files ? inputElement.files : [];
        return Array.from(files).map(f => ({
            relativePath: f.webkitRelativePath || f.name,
            size: f.size,
            lastModified: new Date(f.lastModified).toISOString(),
            contentType: f.type || null
        }));
    }
}

window.MudExFileSystemAccess = MudExFileSystemAccess;
