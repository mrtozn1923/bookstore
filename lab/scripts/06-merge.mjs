// tailwind-merge: the later class wins, conflicting earlier ones are removed.
import { twMerge, extendTailwindMerge } from "tailwind-merge";

const cases = [
    ["bg-brand-600 px-6 py-2", "bg-red-600 px-4"],
    ["grid", "flex"],
    ["p-4", "px-2"],
    ["px-2", "p-4"],
    ["text-lg text-ink-900", "text-hero"],
    ["hover:bg-brand-700", "hover:bg-red-700"],
];
for (const [base, extra] of cases) {
    console.log(`twMerge("${base}", "${extra}")`.padEnd(54), "→", twMerge(base, extra));
}

// teach tailwind-merge Bookstore's custom font size, otherwise text-hero is read as a color
const merge = extendTailwindMerge({ extend: { theme: { text: ["hero"] } } });
console.log(`extended: ("text-lg text-ink-900", "text-hero")`.padEnd(54), "→", merge("text-lg text-ink-900", "text-hero"));
