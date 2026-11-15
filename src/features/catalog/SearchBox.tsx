type SearchBoxProps = {
  value: string;
  onChange: (value: string) => void;
};

// Controlled input: the parent owns the text, the input only shows it and reports changes
export function SearchBox({ value, onChange }: SearchBoxProps) {
  return (
    <input type="search" placeholder="Search by title or author" aria-label="Search books"
      value={value} onChange={(e) => onChange(e.target.value)} />
  );
}
