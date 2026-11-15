// O19: run with `node O19/examples.js` (Node.js 24+), or paste the blocks into the browser console in order
// Outputs of Intl APIs come from the engine's CLDR data and can differ slightly between engines and versions.

// --- example 1 ---
console.log(["Volume 10", "Volume 2", "Volume 1"].sort());
console.log("İSTANBUL".toLowerCase() === "istanbul");
console.log("👍🏽".length);

// --- example 2 ---
const authors = ["Orhan Pamuk", "Çetin Altan", "Ömer Seyfettin", "İlhan Berk",
  "Işıl Özgentürk", "Sait Faik", "Şebnem İşigüzel", "Cemal Süreya"];

console.log([...authors].sort());
// ["Cemal Süreya", "Işıl Özgentürk", "Orhan Pamuk", "Sait Faik", "Çetin Altan", "Ömer Seyfettin", "İlhan Berk", "Şebnem İşigüzel"]

const trCollator = new Intl.Collator("tr");
console.log([...authors].sort(trCollator.compare));
// ["Cemal Süreya", "Çetin Altan", "Işıl Özgentürk", "İlhan Berk", "Orhan Pamuk", "Ömer Seyfettin", "Sait Faik", "Şebnem İşigüzel"]

console.log([...authors].sort(new Intl.Collator("en").compare));
// ["Cemal Süreya", "Çetin Altan", "İlhan Berk", "Işıl Özgentürk", "Ömer Seyfettin", "Orhan Pamuk", "Sait Faik", "Şebnem İşigüzel"]

// --- example 3 ---
const volumes = ["Volume 10", "Volume 2", "Volume 1"];
const numericCollator = new Intl.Collator("en", { numeric: true });
console.log(volumes.toSorted(numericCollator.compare)); // ["Volume 1", "Volume 2", "Volume 10"]

const trBase = new Intl.Collator("tr", { sensitivity: "base" });
const enBase = new Intl.Collator("en", { sensitivity: "base" });

console.log(trBase.compare("Dune", "dune"));         // 0
console.log(trBase.compare("istanbul", "İSTANBUL")); // 0
console.log(trBase.compare("çeşme", "cesme"));       // 1
console.log(enBase.compare("çeşme", "cesme"));       // 0

// --- example 4 ---
function matchesQuery(title, query) {
  return title.toLocaleLowerCase("tr").includes(query.toLocaleLowerCase("tr"));
}

console.log(matchesQuery("Üç İstanbul", "İSTANBUL"));            // true
console.log("Üç İstanbul".toLowerCase().includes("istanbul"));  // false

// --- example 5 ---
const cartMessages = {
  en: { one: "{count} book in your cart", other: "{count} books in your cart" },
  tr: { one: "Sepetinizde {count} kitap var", other: "Sepetinizde {count} kitap var" }
};
const pluralRules = { en: new Intl.PluralRules("en"), tr: new Intl.PluralRules("tr") };

function cartLabel(lang, count) {
  const category = pluralRules[lang].select(count);
  return cartMessages[lang][category].replace("{count}", count);
}

console.log(cartLabel("en", 1)); // "1 book in your cart"
console.log(cartLabel("en", 3)); // "3 books in your cart"
console.log(cartLabel("en", 0)); // "0 books in your cart"
console.log(cartLabel("tr", 3)); // "Sepetinizde 3 kitap var"
console.log(pluralRules.tr.resolvedOptions().pluralCategories); // ["one", "other"]

// --- example 6 ---
const enOrdinal = new Intl.PluralRules("en", { type: "ordinal" });
const suffixes = { one: "st", two: "nd", few: "rd", other: "th" };
const rankLabel = (n) => `${n}${suffixes[enOrdinal.select(n)]}`;

console.log([1, 2, 3, 4, 11, 12, 13, 21, 22, 23].map(rankLabel));
// ["1st", "2nd", "3rd", "4th", "11th", "12th", "13th", "21st", "22nd", "23rd"]

console.log(new Intl.PluralRules("tr", { type: "ordinal" }).resolvedOptions().pluralCategories);
// ["other"]

// --- example 7 ---
const cartTitles = ["Dune", "Emma", "The Hobbit"];

console.log(new Intl.ListFormat("tr").format(cartTitles));    // "Dune, Emma ve The Hobbit"
console.log(new Intl.ListFormat("en").format(cartTitles));    // "Dune, Emma, and The Hobbit"
console.log(new Intl.ListFormat("en-GB").format(cartTitles)); // "Dune, Emma and The Hobbit"
console.log(new Intl.ListFormat("tr", { type: "disjunction" }).format(["Dune", "Emma"])); // "Dune veya Emma"
console.log(new Intl.ListFormat("en", { type: "disjunction" }).format(["Dune", "Emma"])); // "Dune or Emma"

// --- example 8 ---
const languageCodes = ["tr", "en"];
console.log(languageCodes.map((code) => new Intl.DisplayNames([code], { type: "language" }).of(code)));
// ["Türkçe", "English"]

const trRegions = new Intl.DisplayNames(["tr"], { type: "region" });
console.log(["TR", "DE", "US"].map((code) => trRegions.of(code)));
// ["Türkiye", "Almanya", "Amerika Birleşik Devletleri"]

console.log(new Intl.DisplayNames(["en"], { type: "currency" }).of("TRY")); // "Turkish Lira"
console.log(new Intl.DisplayNames(["tr"], { type: "currency" }).of("TRY")); // "Türk lirası"
console.log(new Intl.DisplayNames(["en"], { type: "region" }).of("XX"));    // "XX"
console.log(new Intl.DisplayNames(["en"], { type: "region", fallback: "none" }).of("XX")); // undefined

const trLocale = new Intl.Locale("tr-TR");
console.log(trLocale.language, trLocale.region, trLocale.baseName); // tr TR tr-TR

// --- example 9 ---
const graphemes = new Intl.Segmenter("en", { granularity: "grapheme" });

function visibleLength(text) {
  let count = 0;
  for (const _ of graphemes.segment(text)) count++;
  return count;
}

const review = "Great book 👍🏽";
console.log(review.length, [...review].length, visibleLength(review)); // 15 13 12

const family = "👨‍👩‍👧🇹🇷";
console.log(family.length, [...family].length, visibleLength(family)); // 12 7 2

// --- example 10 ---
function truncate(text, max) {
  const parts = Array.from(graphemes.segment(text), (s) => s.segment);
  return parts.length <= max ? text : parts.slice(0, max).join("") + "…";
}

const shortReview = "Loved it 👨‍👩‍👧 so much";
console.log(JSON.stringify(shortReview.slice(0, 10))); // "Loved it \ud83d"
console.log(truncate(shortReview, 10));                 // "Loved it 👨‍👩‍👧…"

// --- example 11 ---
const trWords = new Intl.Segmenter("tr", { granularity: "word" });
const wordCount = (text) => [...trWords.segment(text)].filter((s) => s.isWordLike).length;

console.log(wordCount("Dune'u iki günde bitirdim, harika!")); // 5

// --- example 12 ---
const shortDuration = {
  tr: new Intl.DurationFormat("tr", { style: "short" }),
  en: new Intl.DurationFormat("en", { style: "short" })
};

function readingTime(lang, totalMinutes) {
  const duration = { hours: Math.floor(totalMinutes / 60), minutes: totalMinutes % 60 };
  return shortDuration[lang].format(duration);
}

console.log(readingTime("tr", 135)); // "2 sa. 15 dk."
console.log(readingTime("en", 135)); // "2 hr, 15 min"
console.log(readingTime("en", 60));  // "1 hr"
console.log(new Intl.DurationFormat("en").format({ minutes: 135 })); // "135 min"
console.log(new Intl.DurationFormat("tr", { style: "long" }).format({ hours: 12, minutes: 40 })); // "12 saat 40 dakika"
console.log(new Intl.DurationFormat("en", { style: "digital" }).format({ hours: 2, minutes: 15 })); // "2:15:00"

// --- example 13 ---
const sampleWords = ["Dune", "Emma", "Middlemarch", "Hobbit", "Frankenstein", "Pride", "Prejudice", "Persuasion", "Ulysses", "Walden"];
const manyTitles = Array.from({ length: 40000 }, (_, i) => `${sampleWords[i % 10]} ${sampleWords[(i * 7) % 10]} ${i}`);

console.time("localeCompare");
[...manyTitles].sort((a, b) => a.localeCompare(b, "tr"));
console.timeEnd("localeCompare");

console.time("one Collator");
[...manyTitles].sort(trCollator.compare);
console.timeEnd("one Collator");

console.time("new Collator per comparison");
[...manyTitles].sort((a, b) => new Intl.Collator("tr").compare(a, b));
console.timeEnd("new Collator per comparison");

// --- example 14 ---
const intlCache = new Map();

function getIntl(Ctor, locale, options = {}) {
  const key = `${Ctor.name}|${locale}|${JSON.stringify(options)}`;
  let instance = intlCache.get(key);
  if (!instance) {
    instance = new Ctor(locale, options);
    intlCache.set(key, instance);
  }
  return instance;
}

console.log(getIntl(Intl.Collator, "tr") === getIntl(Intl.Collator, "tr")); // true
console.log(getIntl(Intl.ListFormat, "en").format(["Dune", "Emma"]));     // "Dune and Emma"
