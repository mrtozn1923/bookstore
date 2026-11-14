type RepeatFast<S extends string, N extends number, Count extends unknown[] = [], Acc extends string = ""> =
  Count["length"] extends N ? Acc : RepeatFast<S, N, [...Count, unknown], `${Acc}${S}`>;

type F999 = RepeatFast<"a", 999>; // fine

// Error: Type instantiation is excessively deep and possibly infinite.
type F1000 = RepeatFast<"a", 1000>;
