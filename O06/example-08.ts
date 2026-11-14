type Preferences = {
  language: "tr" | "en";
  pageSize: number;
  newsletter: boolean;
};

const preferences: Preferences = { language: "tr", pageSize: 20, newsletter: true };

function updatePreference<K extends keyof Preferences>(key: K, value: Preferences[K]) {
  preferences[key] = value;
}

function getPreference<K extends keyof Preferences>(key: K): Preferences[K] {
  return preferences[key];
}

updatePreference("pageSize", 50);
updatePreference("language", "en");
// Error: Argument of type '20' is not assignable to parameter of type '"en" | "tr"'.
updatePreference("language", 20);

const size = getPreference("pageSize"); // number
console.log(size); // 50
