// Origin Private File System: files only this origin can see, not the user's disk
export async function saveSampleChapter() {
  const pdf = new Uint8Array(2 * 1024 * 1024).fill(37); // a 2 MB stand-in for the sample chapter of Dune
  const root = await navigator.storage.getDirectory();
  const samples = await root.getDirectoryHandle("samples", { create: true });
  const handle = await samples.getFileHandle("dune-chapter-1.pdf", { create: true });

  const started = performance.now();
  const writable = await handle.createWritable();
  await writable.write(pdf);
  await writable.close(); // the file changes only when the stream closes
  const file = await handle.getFile();
  console.log(`main thread: wrote ${file.size} bytes in ${(performance.now() - started).toFixed(1)} ms`);

  const names = [];
  for await (const name of samples.keys()) names.push(name);
  console.log("samples/:", names.join(", "));
}

export function readSampleChapterInWorker() {
  const worker = new Worker(new URL("./opfs-worker.js", import.meta.url), { type: "module" });
  return new Promise((resolve) => {
    worker.onmessage = ({ data }) => {
      console.log(data);
      worker.terminate();
      resolve(data);
    };
    worker.postMessage("samples/dune-chapter-1.pdf");
  });
}
