// The Navigation API: one navigate event for links, forms, back/forward and navigation.navigate()
export const log = [];

export function interceptNavigations() {
  navigation.addEventListener("navigate", (event) => {
    const url = new URL(event.destination.url);
    log.push(`navigate ${url.pathname}${url.search} type=${event.navigationType} canIntercept=${event.canIntercept}`);
    // only our own "pages" under /tabs/; anything else is a normal document load
    if (!event.canIntercept || url.pathname !== "/tabs/" || event.downloadRequest !== null) return;
    event.intercept({
      async handler() {
        await new Promise((resolve) => setTimeout(resolve, 50)); // as if data were loading
        document.querySelector("#view").textContent = `Page: ${url.searchParams.get("page") ?? "home"}`;
      },
    });
  });
}

export async function goTo(page) {
  const { committed, finished } = navigation.navigate(`/tabs/?page=${page}`);
  await committed;
  await finished;
  return { url: navigation.currentEntry.url.replace(location.origin, ""), entries: navigation.entries().length };
}
