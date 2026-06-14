const envApiUrl = import.meta.env.VITE_API_URL as string | undefined;
if (!envApiUrl) {
  throw new Error("VITE_API_URL is not defined");
}

const delayEnabled = false;
const apiLoggingEnabled = true;

const delays = { fast: 600, slow: 1200, slowest: 1800 } as const;
export const ApiUrl = envApiUrl;
export const BaseUrl = ApiUrl.replace(/\/api\/?$/, "");

export class ApiAuthError extends Error {
  constructor() {
    super("Unauthorized");
    this.name = "ApiAuthError";
  }
}

const redirectToLogin = () => {
  const returnTo = `${window.location.pathname}${window.location.search}`;
  const safeReturnTo = returnTo.startsWith("/redirecting") ? "/" : returnTo;
  window.location.assign(`${BaseUrl}/auth/login?returnUrl=${encodeURIComponent(safeReturnTo)}`);
};

export const redirectIfUnauthorized = (response: Response): void => {
  if (response.status !== 401) {
    return;
  }

  redirectToLogin();
  throw new ApiAuthError();
};

export const withOptionalDelay = async <T>(delay: keyof typeof delays, fn: () => Promise<T>): Promise<T> => {
  if (delayEnabled) {
    await new Promise<void>((resolve) => setTimeout(resolve, delays[delay]));
  }
  return fn();
};

const withErrorHandling = async <T>(fn: () => Promise<T>, label: string): Promise<T> => {
  try {
    return await fn();
  } catch (error) {
    if (error instanceof ApiAuthError) {
      throw error;
    }
    const message = error instanceof Error ? error.message : String(error);
    console.error(`[API ERROR] ${label}: ${message}`);
    throw error;
  }
};

export const parseApiErrorMessage = (error: unknown, fallback: string): string => {
  if (error instanceof ApiAuthError) {
    return fallback;
  }

  if (!(error instanceof Error)) {
    return fallback;
  }

  const quoted = error.message.match(/: "([^"]+)"/);
  if (quoted?.[1]) {
    return quoted[1];
  }

  const plain = error.message.match(/:\s(.+)$/);
  if (plain?.[1]) {
    return plain[1].trim();
  }

  return error.message || fallback;
};

export const customFetch = async <T>(input: RequestInfo | URL, init?: RequestInit): Promise<T> => {
  const label = `${init?.method ?? "GET"} ${input}`;
  const start = apiLoggingEnabled ? performance.now() : 0;

  return withErrorHandling(async () => {
    const response = await fetch(input, { credentials: "include", ...init });
    if (!response.ok) {
      redirectIfUnauthorized(response);

      let details = "";
      try {
        const text = await response.text();
        details = text ? `: ${text}` : "";
      } catch {
        // ignore
      }
      throw new Error(`${response.status} ${response.statusText}${details}`);
    }
    if (apiLoggingEnabled) {
      console.log(`${label} (${(performance.now() - start).toFixed(1)}ms)`);
    }
    if (response.status === 204) {
      return undefined as T;
    }
    const text = await response.text();
    if (!text) {
      return undefined as T;
    }
    return JSON.parse(text) as T;
  }, label);
};

export const EmployeeTypeAcademicId = "00000000-0000-0000-0000-000000000001";
export const EmployeeTypeNonAcademicId = "00000000-0000-0000-0000-000000000002";
