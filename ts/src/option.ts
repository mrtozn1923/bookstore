export type Option<T> = { readonly kind: "some"; readonly value: T } | { readonly kind: "none" };

export const some = <T>(value: T): Option<T> => ({ kind: "some", value });
export const none: Option<never> = { kind: "none" };

export const map = <T, U>(option: Option<T>, f: (value: T) => U): Option<U> =>
  option.kind === "some" ? some(f(option.value)) : none;

export const bind = <T, U>(option: Option<T>, f: (value: T) => Option<U>): Option<U> =>
  option.kind === "some" ? f(option.value) : none;

export const getOrElse = <T>(option: Option<T>, fallback: T): T =>
  option.kind === "some" ? option.value : fallback;

export function find<K, V>(source: ReadonlyMap<K, V>, key: K): Option<V> {
  return source.has(key) ? some(source.get(key) as V) : none;
}
