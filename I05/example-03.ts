type Nested = string | Nested[]; // fine: the reference sits behind an array

// Error: Type alias 'Loop' circularly references itself.
type Loop = Loop | string;
