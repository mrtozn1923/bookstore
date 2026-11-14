// Request streams need duplex: "half"; browsers without support ignore the getter
// and turn the stream into the string "[object ReadableStream]" (which sets a Content-Type)
export const supportsRequestStreams = (() => {
  let duplexRead = false;
  const request = new Request("/", {
    method: "POST",
    body: new ReadableStream(),
    get duplex() {
      duplexRead = true;
      return "half";
    },
  });
  return duplexRead && !request.headers.has("Content-Type");
})();

// Counts the bytes the network stack pulls from the stream: upload progress without XHR
function countBytes(onProgress) {
  let sent = 0;
  return new TransformStream({
    transform(chunk, controller) {
      sent += chunk.byteLength;
      onProgress(sent);
      controller.enqueue(chunk);
    },
  });
}

export async function uploadCoverStream(isbn, file, base = "") {
  const body = file.stream().pipeThrough(countBytes((sent) => console.log(`sent ${sent} of ${file.size}`)));
  const response = await fetch(`${base}/api/covers/${isbn}`, {
    method: "POST",
    headers: { "Content-Type": file.type },
    body,
    duplex: "half",
  });
  return response.json();
}
