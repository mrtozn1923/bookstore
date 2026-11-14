type Preferences = {
  language: "tr" | "en";
  pageSize: number;
  newsletter: boolean;
};

type PreferenceValue = Preferences[keyof Preferences];
// number | "en" | "tr" | boolean

function updatePreferenceLoose(key: keyof Preferences, value: PreferenceValue) {
  console.log(key, value);
}

updatePreferenceLoose("language", 20); // compiles, but makes no sense
