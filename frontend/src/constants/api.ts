const envApiUrl = import.meta.env.VITE_API_URL as string | undefined;
if (!envApiUrl) {
  throw new Error("VITE_API_URL is not defined");
}

export const ApiUrl = envApiUrl;
export const BaseUrl = ApiUrl.replace(/\/api\/?$/, "");

/** Flip to true when testing loading states at different speeds. */
const delayEnabled = false;
const delayMs = { fast: 600, slow: 1200, slowest: 1800 } as const;

export const withDelay = async <T>(speed: keyof typeof delayMs, fn: () => Promise<T>): Promise<T> => {
  if (delayEnabled) {
    await new Promise<void>((resolve) => setTimeout(resolve, delayMs[speed]));
  }
  return fn();
};

export const goToLogin = () => {
  const returnTo = `${window.location.pathname}${window.location.search}`;
  const safeReturnTo = returnTo.startsWith("/redirecting") ? "/" : returnTo;
  window.location.assign(`${BaseUrl}/auth/login?returnUrl=${encodeURIComponent(safeReturnTo)}`);
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
  const response = await fetch(input, { credentials: "include", ...init });
  if (response.status === 401) {
    goToLogin();
    return new Promise<T>(() => {});
  }
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
