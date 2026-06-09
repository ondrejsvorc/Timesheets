import type { CurrentUserPermissions } from "./api/getCurrentUserPermissions";

export type RoleViewMode = "actual" | "employee" | "globalManager" | "projectManager" | "contractManager" | "roleManager";

export interface RoleViewState {
  mode: RoleViewMode;
  projectId: string | null;
  contractId: string | null;
}

const STORAGE_KEY = "timesheets:role-view";

export const defaultRoleViewState = (): RoleViewState => ({
  mode: "actual",
  projectId: null,
  contractId: null,
});

export const loadRoleViewState = (): RoleViewState => {
  if (typeof window === "undefined") {
    return defaultRoleViewState();
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return defaultRoleViewState();
    }

    const parsed = JSON.parse(raw) as Partial<RoleViewState>;
    return {
      mode: parsed.mode ?? "actual",
      projectId: parsed.projectId ?? null,
      contractId: parsed.contractId ?? null,
    };
  } catch {
    return defaultRoleViewState();
  }
};

export const saveRoleViewState = (state: RoleViewState) => {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
};

export const applyRoleViewOverride = (actual: CurrentUserPermissions | null, roleView: RoleViewState): CurrentUserPermissions | null => {
  if (!actual || roleView.mode === "actual") {
    return actual;
  }

  const base: CurrentUserPermissions = {
    isRoleManager: false,
    isGlobalManager: false,
    projectManagerOf: [],
    contractManagerOf: [],
  };

  switch (roleView.mode) {
    case "employee":
      return base;
    case "globalManager":
      return { ...base, isGlobalManager: true };
    case "projectManager":
      return {
        ...base,
        projectManagerOf: roleView.projectId ? [roleView.projectId] : [],
      };
    case "contractManager":
      return {
        ...base,
        contractManagerOf: roleView.contractId ? [roleView.contractId] : [],
      };
    case "roleManager":
      return { ...base, isRoleManager: true };
    default:
      return actual;
  }
};

export const isRoleViewOverridden = (roleView: RoleViewState) => roleView.mode !== "actual";
