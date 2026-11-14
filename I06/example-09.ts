interface Producer<out T> {
  produce: () => T;
}
interface Consumer<in T> {
  consume: (value: T) => void;
}
interface Store<in out T> {
  get: () => T;
  set: (value: T) => void;
}

// Error: Type 'WrongProducer<sub-T>' is not assignable to type 'WrongProducer<super-T>' as implied by variance annotation.
interface WrongProducer<out T> {
  consume: (value: T) => void;
}
