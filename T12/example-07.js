const preferences = { language: "en", currency: "TRY", newsletter: undefined };

console.log("language" in preferences);            // true
console.log("theme" in preferences);               // false
console.log("newsletter" in preferences);          // true
console.log(preferences.newsletter !== undefined); // false
