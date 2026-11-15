import { Component, createContext, createRef, useContext, useRef, type Ref } from 'react';
import { createRoot } from 'react-dom/client';

const ThemeContext = createContext('light');

// A default parameter instead of defaultProps
function PriceTag({ price, currency = 'TRY' }: { price: number; currency?: string }) {
  const theme = useContext(ThemeContext);
  return <span className={theme}>{price} {currency}</span>;
}

// ref is an ordinary prop: no forwardRef
function SearchBox({ ref, ...props }: { placeholder: string; ref?: Ref<HTMLInputElement> }) {
  return <input ref={ref} {...props} />;
}

// createRef instead of a string ref
class BookTitle extends Component<{ title: string }> {
  heading = createRef<HTMLHeadingElement>();
  componentDidMount() {
    this.heading.current!.dataset.ready = 'yes';
  }
  render() {
    return <h2 ref={this.heading}>{this.props.title}</h2>;
  }
}

export function App() {
  const search = useRef<HTMLInputElement>(null);
  const renders = useRef<number>(undefined);
  renders.current = (renders.current ?? 0) + 1;
  let box: HTMLDivElement | null = null;
  return (
    <ThemeContext value="dark">
      <div ref={(node) => { box = node; }} onClick={() => box?.focus()}>
        <BookTitle title="Dune" />
        <PriceTag price={489} />
        <SearchBox ref={search} placeholder="Search" />
      </div>
    </ThemeContext>
  );
}

export function mount(container: HTMLElement) {
  createRoot(container).render(<App />);
}
