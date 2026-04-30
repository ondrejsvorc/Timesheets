import { useLocation, useNavigate } from "react-router";

type NavigateOptions = {
  replace?: boolean;
  state?: unknown;
  preventScrollReset?: boolean;
  relative?: "route" | "path";
};

/**
 * Wraps `navigate()` and automatically passes `{ state: { from } }`.
 */
export const useNavigateFrom = () => {
  const navigate = useNavigate();
  const location = useLocation();

  return (to: string, options?: NavigateOptions) => {
    const from = `${location.pathname}${location.search}`;
    const state = { ...(options?.state as object | null), from };
    navigate(to, { ...options, state });
  };
};
