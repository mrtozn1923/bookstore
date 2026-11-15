import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    root: "02-vite",
    plugins: [tailwindcss()],
});
