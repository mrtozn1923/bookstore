type Size = "sm" | "lg";
type Color = "red" | "blue";
type ClassName = `btn-${Size}-${Color}`;
// "btn-lg-blue" | "btn-lg-red" | "btn-sm-blue" | "btn-sm-red"

// Error: Type '"btn-md-red"' is not assignable to type '"btn-lg-blue" | "btn-lg-red" | "btn-sm-blue" | "btn-sm-red"'. Did you mean '"btn-sm-red"'?
const className: ClassName = "btn-md-red";
