// The same file through the legacy API (render) and the modern API (compile).
import * as sass from "sass";

const file = "07/migrated/checkout.scss";
// make every deprecation that becomes an error in Dart Sass 2.0 fatal today
const fatalDeprecations = [sass.deprecations.dartSass2];

const legacy = sass.renderSync({ file });
console.log(`legacy renderSync:         ${legacy.css.length} bytes`);

try {
    sass.renderSync({ file, fatalDeprecations });
} catch (error) {
    console.log(`legacy renderSync, fatal:  ${error.message.split("\n")[0]}`);
}

const modern = sass.compile(file, { fatalDeprecations });
console.log(`modern compile, fatal:     ${modern.css.length} bytes, ${modern.loadedUrls.length} files loaded`);
