async function loadPreferences() {
  return { language: "tr", pageSize: 20 };
}

type Pending = ReturnType<typeof loadPreferences>;
// Promise<{ language: string; pageSize: number; }>

type Preferences = Awaited<ReturnType<typeof loadPreferences>>;
// { language: string; pageSize: number; }

type Nested = Awaited<Promise<Promise<number>>>; // number
type Plain = Awaited<string>; // string

function render(preferences: Preferences) {
  console.log(`${preferences.language} / ${preferences.pageSize}`);
}

loadPreferences().then(render); // tr / 20
