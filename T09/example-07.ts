function welcome(name?: string | null) {
  if (name == null) {
    return "Welcome, guest!";
  }
  // name: string
  return `Welcome back, ${name}!`;
}
