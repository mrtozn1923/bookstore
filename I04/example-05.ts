type Px = `${number}px`;

const width: Px = "12px";
const offset: Px = "-1.5px";

// Error: Type '"px"' is not assignable to type '`${number}px`'.
const empty: Px = "px";

// Error: Type '"abcpx"' is not assignable to type '`${number}px`'.
const wrong: Px = "abcpx";
