// Writes 40 small component style sheets and a main.js that imports them all,
// to give the Sass compiler something to do in the build.
import { mkdir, writeFile } from "node:fs/promises";

const count = 40;
await mkdir("src/components", { recursive: true });
const imports = [];
for (let i = 1; i <= count; i++) {
    const name = `card-${String(i).padStart(2, "0")}`;
    await writeFile(`src/components/${name}.scss`, `.${name} {
    padding: bs.space(3);

    @include bs.up("md") {
        padding: bs.space(4);
    }

    &__title {
        font-size: bs.rem(${16 + (i % 6)}px);
        color: bs.tone(#b45309, -${i % 10}%);
    }
}
`);
    imports.push(`import "./components/${name}.scss";`);
}
await writeFile("src/main.js", `import "./styles/main.scss";\n${imports.join("\n")}\n`);
console.log(`${count} component style sheets written`);
