import { Component, type ErrorInfo, type ReactNode } from 'react';

type ErrorBoundaryProps = {
  fallback: (error: Error) => ReactNode;
  children: ReactNode;
};

type ErrorBoundaryState = { error: Error | null };

// Error boundaries are still class components: there is no hook for getDerivedStateFromError
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[boundary]', error.message, info.componentStack?.trim().split('\n')[0]);
  }

  render() {
    return this.state.error ? this.props.fallback(this.state.error) : this.props.children;
  }
}
