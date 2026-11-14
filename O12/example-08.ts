async function fetchTitle(): Promise<string> {
  throw new Error("Network down");
}

async function withoutAwait(): Promise<string> {
  try {
    return fetchTitle();
  } catch {
    return "fallback";
  }
}

async function withAwait(): Promise<string> {
  try {
    return await fetchTitle();
  } catch {
    return "fallback";
  }
}

withAwait().then((title) => console.log("withAwait:", title));
withoutAwait()
  .then((title) => console.log("withoutAwait:", title))
  .catch((reason) => console.log("rejected:", reason.message));
