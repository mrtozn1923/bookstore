// createSyncAccessHandle exists only in dedicated workers: synchronous, fast reads and writes
self.onmessage = async ({ data: path }) => {
  const [dir, name] = path.split("/");
  const root = await navigator.storage.getDirectory();
  const handle = await (await root.getDirectoryHandle(dir)).getFileHandle(name);
  const access = await handle.createSyncAccessHandle(); // locks the file until close()
  try {
    const size = access.getSize();
    const buffer = new Uint8Array(size);
    const started = performance.now();
    const read = access.read(buffer, { at: 0 });
    self.postMessage(`worker: read ${read} of ${size} bytes in ${(performance.now() - started).toFixed(1)} ms, first byte ${buffer[0]}`);
  } finally {
    access.close();
  }
};
