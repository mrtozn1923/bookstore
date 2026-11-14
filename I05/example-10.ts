type Repeat<S extends string, N extends number, Count extends unknown[] = []> =
  Count["length"] extends N ? "" : `${S}${Repeat<S, N, [...Count, unknown]>}`;

type R47 = Repeat<"a", 47>; // fine

// Error: Type instantiation is excessively deep and possibly infinite.
type R48 = Repeat<"a", 48>;
