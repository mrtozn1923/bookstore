type Repeat<S extends string, N extends number, Count extends unknown[] = []> =
  Count["length"] extends N ? "" : `${S}${Repeat<S, N, [...Count, unknown]>}`;

type Three = Repeat<"ab", 3>; // "ababab"
