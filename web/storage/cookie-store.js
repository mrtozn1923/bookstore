// cookieStore: an asynchronous cookie API with change events (also available in service workers)
export async function languageCookie() {
  cookieStore.addEventListener("change", (event) => {
    for (const cookie of event.changed) console.log(`change: ${cookie.name}=${cookie.value}`);
    for (const cookie of event.deleted) console.log(`deleted: ${cookie.name}`);
  });

  await cookieStore.set({ name: "lang", value: "tr", expires: Date.now() + 365 * 24 * 60 * 60 * 1000, sameSite: "lax" });
  const lang = await cookieStore.get("lang");
  console.log(`get: ${lang.name}=${lang.value}, sameSite ${lang.sameSite}, path ${lang.path}`);
  console.log(`document.cookie: ${document.cookie}`);
  await cookieStore.delete("lang");
  await new Promise((resolve) => setTimeout(resolve, 100)); // change events are asynchronous
}
