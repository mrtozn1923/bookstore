function echoUnknown(value: unknown): unknown {
  return value;
}

const u = echoUnknown(450);
// Error: 'u' is of type 'unknown'.
u.toFixed(2);
