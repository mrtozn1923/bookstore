function formatDate(value: Date | string) {
  if (value instanceof Date) {
    // value: Date
    return value.toISOString();
  }
  // value: string
  return value;
}
