type FirstChar<S extends string> = S extends `${infer C}${string}` ? C : never;

type Rocket = FirstChar<"🚀 launch">;
const ok: Rocket = "🚀";
