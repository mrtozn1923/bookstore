// /api/books answers with Cache-Control: max-age=60 and an ETag
export async function cacheModes() {
  for (const cache of ["default", "default", "no-cache", "no-store", "reload", "force-cache"]) {
    const response = await fetch("/api/books", { cache });
    console.log(`cache: "${cache}" -> ${response.status}`);
    await new Promise((resolve) => setTimeout(resolve, 100)); // keep the server log in order
  }
}
