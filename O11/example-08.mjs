try {
  const response = await fetch("https://openlibrary.org/search.json?author=jane+austen&fields=key,title&limit=100", {
    signal: AbortSignal.timeout(5000),
  });
  const result = await response.json();
  console.log(result.docs.length); // 100
} catch (error) {
  if (error.name === "TimeoutError") {
    console.error("The server didn't answer within 5 seconds");
  } else if (error.name === "AbortError") {
    console.error("The request was cancelled");
  } else {
    console.error("Network error:", error.message);
  }
}
