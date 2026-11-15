import type { Ref } from 'react';

type SearchBoxProps = {
  value: string;
  onChange: (value: string) => void;
  // React 19: ref is an ordinary prop, no forwardRef needed
  ref?: Ref<HTMLInputElement>;
};

// Controlled input: the parent owns the text, the input only shows it and reports changes
export function SearchBox({ value, onChange, ref }: SearchBoxProps) {
  return (
    <input ref={ref} type="search" placeholder="Search by title or author (press /)" aria-label="Search books"
      value={value} onChange={(e) => onChange(e.target.value)} />
  );
}
