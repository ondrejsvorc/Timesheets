const envApiUrl = import.meta.env.VITE_API_URL as string | undefined;
if (!envApiUrl) {
  throw new Error("VITE_API_URL is not defined");
}

export const ApiUrl = envApiUrl;
export const BaseUrl = ApiUrl.replace(/\/api\/?$/, "");

/** Flip to true when testing loading states at different speeds. */
const delayEnabled = false;
const delayMs = { fast: 600, slow: 1200, slowest: 1800 } as const;

const sessionExpiredHandlers = new Set<() => void>();
let loginRedirectStarted = false;

if (typeof window !== "undefined") {
  const path = window.location.pathname;
  if (!path.startsWith("/auth/") && path !== "/login-oidc" && path !== "/logout-oidc") {
    loginRedirectStarted = false;
  }
}

export const onSessionExpired = (handler: () => void): (() => void) => {
  sessionExpiredHandlers.add(handler);
  return () => sessionExpiredHandlers.delete(handler);
};

export const withDelay = async <T>(speed: keyof typeof delayMs, fn: () => Promise<T>): Promise<T> => {
  if (delayEnabled) {
    await new Promise<void>((resolve) => setTimeout(resolve, delayMs[speed]));
  }
  return fn();
};

export const goToLogin = (returnTo?: string) => {
  if (loginRedirectStarted || typeof window === "undefined") {
    return;
  }

  const currentPath = window.location.pathname;
  if (currentPath.startsWith("/auth/") || currentPath === "/login-oidc" || currentPath === "/logout-oidc") {
    return;
  }

  loginRedirectStarted = true;
  for (const handler of sessionExpiredHandlers) {
    handler();
  }

  const resolvedReturnTo = returnTo ?? `${window.location.pathname}${window.location.search}`;
  const safeReturnTo = resolvedReturnTo.startsWith("/redirecting") ? "/" : resolvedReturnTo;
  window.location.replace(`${BaseUrl}/auth/login?returnUrl=${encodeURIComponent(safeReturnTo)}`);
};

const isAuthFailure = (response: Response): boolean => response.status === 401;

export const fetchWithAuth = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
  let response: Response;
  try {
    response = await fetch(input, {
      ...init,
      credentials: "include",
    });
  } catch (error) {
    if (error instanceof TypeError) {
      goToLogin();
      return new Promise(() => {});
    }
    throw error;
  }

  if (isAuthFailure(response)) {
    goToLogin();
    return new Promise(() => {});
  }
  return response;
};

export const parseApiErrorMessage = (error: unknown, fallback: string): string => {
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
  const response = await fetchWithAuth(input, init);
  if (!response.ok) {
    let details = "";
    try {
      const text = await response.text();
      details = text ? `: ${text}` : "";
    } catch {
      // ignore
    }
    throw new Error(`${response.status} ${response.statusText}${details}`);
  }
  if (response.status === 204) {
    return undefined as T;
  }
  const text = await response.text();
  if (!text) {
    return undefined as T;
  }
  return JSON.parse(text) as T;
};

export const EmployeeTypeAcademicId = "00000000-0000-0000-0000-000000000001";
export const EmployeeTypeNonAcademicId = "00000000-0000-0000-0000-000000000002";
