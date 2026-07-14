import { useLocation, useNavigate } from "react-router";

type LocationState = { from?: string };

export const useGo = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState | null;

  const stamp = () => `${location.pathname}${location.search}`;

  const forward = (path: string) => {
    navigate(path, { state: { from: stamp() } });
  };

  const back = (fallback: string | (() => string)) => () => {
    if (state?.from) {
      navigate(state.from);
      return;
    }
    navigate(typeof fallback === "function" ? fallback() : fallback);
  };

  return { forward, back };
};
