type Color = string | [number, number, number];
type CategoryColors = Record<"fiction" | "science", Color>;

const categoryColors: CategoryColors = {
  fiction: "#0055ff",
  science: [255, 120, 0],
};

// Error: Property 'toUpperCase' does not exist on type 'Color'.
categoryColors.fiction.toUpperCase();
