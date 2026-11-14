// RFC 9457 Problem Details as Bookstore.Api sends them.
export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
}

const problemBase = 'https://api.example.com/problems/';

export function isProblem(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && 'type' in body && 'status' in body;
}

// The catalog code is the last segment of the type URI: ".../problems/insufficient-stock" -> "insufficient-stock".
export function problemCode(problem: ProblemDetails): string | null {
  return problem.type.startsWith(problemBase) ? problem.type.slice(problemBase.length) : null;
}
