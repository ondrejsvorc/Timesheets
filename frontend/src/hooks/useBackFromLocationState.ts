import { useLocation, useNavigate } from "react-router";

type LocationState = {
  from?: string;
};

type Fallback = string | (() => string);

/**
 * Back navigation that prefers `location.state.from` and falls back to a provided route.
 */
export const useBackFromLocationState = (fallback: Fallback) => {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState | null;

  return () => {
    if (state?.from) {
      navigate(state.from);
      return;
    }
    navigate(typeof fallback === "function" ? fallback() : fallback);
  };
};

