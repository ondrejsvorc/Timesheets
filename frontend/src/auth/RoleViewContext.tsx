import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { CurrentUserPermissions } from "./api";
import { useCurrentUser } from "./CurrentUserContext";
import { applyRoleViewOverride, isRoleViewOverridden, loadRoleViewState, type RoleViewState, saveRoleViewState } from "./roleView";

interface RoleViewContextValue {
  actualPermissions: CurrentUserPermissions | null;
  effectivePermissions: CurrentUserPermissions | null;
  roleView: RoleViewState;
  setRoleView: (next: RoleViewState) => void;
  isOverridden: boolean;
}

const RoleViewContext = createContext<RoleViewContextValue | null>(null);

interface RoleViewProviderProps {
  children: React.ReactNode;
}

export const RoleViewProvider = ({ children }: RoleViewProviderProps) => {
  const actualPermissions = useCurrentUser().permissions;
  const [roleView, setRoleViewState] = useState<RoleViewState>(loadRoleViewState);

  const setRoleView = useCallback((next: RoleViewState) => {
    setRoleViewState(next);
    saveRoleViewState(next);
  }, []);

  const value = useMemo<RoleViewContextValue>(() => {
    const effectivePermissions = applyRoleViewOverride(actualPermissions, roleView);
    return {
      actualPermissions,
      effectivePermissions,
      roleView,
      setRoleView,
      isOverridden: isRoleViewOverridden(roleView),
    };
  }, [actualPermissions, roleView, setRoleView]);

  return <RoleViewContext.Provider value={value}>{children}</RoleViewContext.Provider>;
};

export const useRoleViewContext = () => {
  const context = useContext(RoleViewContext);
  if (!context) {
    throw new Error("useRoleViewContext must be used inside RoleViewProvider");
  }
  return context;
};

export const useEffectivePermissions = () => {
  const { effectivePermissions, roleView, setRoleView, isOverridden, actualPermissions } = useRoleViewContext();
  return { permissions: effectivePermissions, actualPermissions, roleView, setRoleView, isOverridden };
};
