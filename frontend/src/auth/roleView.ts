import type { CurrentUserPermissions } from "./api/getCurrentUserPermissions";
import { UserRole } from "./userRole";

export type RoleViewMode = "actual" | "employee" | "globalManager" | "projectManager" | "contractManager" | "roleManager";

export interface RoleViewState {
  mode: RoleViewMode;
  projectId: string | null;
  contractId: string | null;
}

const STORAGE_KEY = "timesheets:role-view";

const roleViewRole: Record<Exclude<RoleViewMode, "actual">, UserRole> = {
  employee: UserRole.Employee,
  globalManager: UserRole.GlobalManager,
  projectManager: UserRole.ProjectManager,
  contractManager: UserRole.ContractManager,
  roleManager: UserRole.Admin,
};

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

const emptyPermissions = (role: UserRole): CurrentUserPermissions => ({
  role,
  projectManagerOf: [],
  contractManagerOf: [],
  employeeOnContractIds: [],
  visibleProjectIds: [],
  visibleContractIds: [],
});

export const applyRoleViewOverride = (actual: CurrentUserPermissions | null, roleView: RoleViewState): CurrentUserPermissions | null => {
  if (!actual || roleView.mode === "actual") {
    return actual;
  }

  const role = roleViewRole[roleView.mode];
  const base = emptyPermissions(role);

  switch (roleView.mode) {
    case "employee":
      return {
        ...base,
        employeeOnContractIds: actual.employeeOnContractIds,
        visibleContractIds: actual.employeeOnContractIds,
        visibleProjectIds: actual.employeeOnContractIds.length > 0 ? actual.visibleProjectIds.filter((id) => !actual.projectManagerOf.includes(id)) : [],
      };
    case "globalManager":
    case "roleManager":
      return base;
    case "projectManager":
      return {
        ...base,
        projectManagerOf: roleView.projectId ? [roleView.projectId] : [],
        visibleProjectIds: roleView.projectId ? [roleView.projectId] : [],
      };
    case "contractManager":
      return {
        ...base,
        contractManagerOf: roleView.contractId ? [roleView.contractId] : [],
        visibleContractIds: roleView.contractId ? [roleView.contractId] : [],
      };
    default:
      return actual;
  }
};

export const isRoleViewOverridden = (roleView: RoleViewState) => roleView.mode !== "actual";
