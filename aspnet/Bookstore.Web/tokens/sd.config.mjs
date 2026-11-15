// DTCG tokens → a Tailwind @theme block (Styles/theme.css)
import StyleDictionary from "style-dictionary";

StyleDictionary.registerTransform({
    name: "value/dtcg-css",
    type: "value",
    transitive: true,
    transform: (token) => {
        const v = token.$value;
        if (token.$type === "color") return `oklch(${v.components[0]} ${v.components[1]} ${v.components[2]})`;
        if (token.$type === "dimension") return `${v.value}${v.unit}`;
        if (token.$type === "fontFamily") return v.map((f) => (f.includes(" ") ? `"${f}"` : f)).join(", ");
        return v;
    },
});

StyleDictionary.registerFormat({
    name: "css/tailwind-theme",
    format: ({ dictionary }) =>
        "/* generated from tokens/bookstore.tokens.json, do not edit */\n@theme {\n" +
        dictionary.allTokens.map((t) => `    --${t.name}: ${t.$value};`).join("\n") +
        "\n}\n",
});

export default {
    source: ["tokens/bookstore.tokens.json"],
    platforms: {
        tailwind: {
            transforms: ["name/kebab", "value/dtcg-css"],
            buildPath: "Styles/",
            files: [{ destination: "theme.css", format: "css/tailwind-theme" }],
        },
    },
};
