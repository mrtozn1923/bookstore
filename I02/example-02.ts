// Error: 'infer' declarations are only permitted in the 'extends' clause of a conditional type.
type Broken<T> = infer U;
