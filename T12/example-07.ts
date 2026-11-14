type Color = string | [number, number, number];
type CategoryColors = Record<"fiction" | "science", Color>;

const categoryColors = {
  fiction: "#0055ff",
  science: [255, 120, 0],
} satisfies CategoryColors;

console.log(categoryColors.fiction.toUpperCase()); // #0055FF
console.log(categoryColors.science.map((channel) => channel / 255).length); // 3
