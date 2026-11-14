export function showReviews(target) {
  const p = document.createElement("p");
  p.textContent = "The reviews module has just loaded.";
  target.append(p);
}
