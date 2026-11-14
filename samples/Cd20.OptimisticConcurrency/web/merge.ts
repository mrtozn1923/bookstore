export interface MergeResult<T> {
  merged: T;
  conflicts: (keyof T)[];
}

export function threeWayMerge<T extends object>(base: T, mine: T, theirs: T): MergeResult<T> {
  const merged = { ...theirs };
  const conflicts: (keyof T)[] = [];
  for (const field of Object.keys(base) as (keyof T)[]) {
    const iChanged = mine[field] !== base[field];
    const theyChanged = theirs[field] !== base[field];
    if (iChanged && !theyChanged) {
      merged[field] = mine[field];
    } else if (iChanged && theyChanged && mine[field] !== theirs[field]) {
      conflicts.push(field);
    }
  }
  return { merged, conflicts };
}
