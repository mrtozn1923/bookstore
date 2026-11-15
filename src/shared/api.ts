// RFC 9457 Problem Details, as produced by ASP.NET Core's AddProblemDetails()
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

// One place that knows how Bookstore.Api and the BFF answer: JSON (or 204) on success, Problem Details (or nothing) on error
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      Accept: 'application/json',
      // The BFF refuses /api and /bff calls without it (anti-CSRF); every path here is on our own origin
      'X-CSRF': '1',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
    },
  });
  if (!response.ok) {
    const isProblem = response.headers.get('Content-Type')?.includes('application/problem+json') ?? false;
    throw new ApiError(response.status, isProblem ? ((await response.json()) as ProblemDetails) : null);
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}
