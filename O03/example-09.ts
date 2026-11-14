class Settings<T> {
  // Error: Static members cannot reference class type parameters.
  static defaults: T;
}
