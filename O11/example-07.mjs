const controller = new AbortController();

setTimeout(() => controller.abort(), 50); // give up after 50 ms

try {
  const response = await fetch("https://openlibrary.org/search.json?author=jane+austen&limit=1000", {
    signal: controller.signal,
  });
  console.log(response.status);
} catch (error) {
  console.log(error.name); // most likely "AbortError"
}
