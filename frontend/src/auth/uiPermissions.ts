import type { CurrentUserPermissions } from "./api/getCurrentUserPermissions";

export const UiAction = {
  nav: {
    projects: "nav.projects",
    employees: "nav.employees",
    myTimesheets: "nav.myTimesheets",
    employeeRoles: "nav.employeeRoles",
  },
  projects: {
    add: "projects.add",
    edit: "projects.edit",
    delete: "projects.delete",
    view: "projects.view",
  },
  contracts: {
    add: "contracts.add",
    edit: "contracts.edit",
    delete: "contracts.delete",
    view: "contracts.view",
  },
  contractManagers: {
    view: "contractManagers.view",
    add: "contractManagers.add",
    remove: "contractManagers.remove",
  },
  contractEmployees: {
    view: "contractEmployees.view",
    add: "contractEmployees.add",
    update: "contractEmployees.update",
    remove: "contractEmployees.remove",
  },
  employeePositions: {
    add: "employeePositions.add",
  },
  employees: {
    list: "employees.list",
    view: "employees.view",
    editType: "employees.editType",
  },
  timesheet: {
    view: "timesheet.view",
    import: "timesheet.import",
    edit: "timesheet.edit",
    submit: "timesheet.submit",
    approveProject: "timesheet.approveProject",
    returnProject: "timesheet.returnProject",
    finalApprove: "timesheet.finalApprove",
    returnWhole: "timesheet.returnWhole",
    unlock: "timesheet.unlock",
  },
} as const;

type DeepValues<T> = T extends string ? T : T extends object ? DeepValues<T[keyof T]> : never;

export type UiActionId = DeepValues<typeof UiAction>;

export interface UiContext {
  projectId?: string;
  contractId?: string;
  employeeId?: string;
  timesheetContractId?: string;
  timesheetProjectId?: string;
}

export const hasGlobalScope = (permissions: CurrentUserPermissions | null): boolean =>
  Boolean(permissions?.isGlobalManager || permissions?.isRoleManager);

export const isOwnEmployee = (currentUserId: string | undefined, employeeId: string | undefined): boolean =>
  Boolean(currentUserId && employeeId && currentUserId === employeeId);

export const canAccessProject = (permissions: CurrentUserPermissions | null, projectId: string | undefined): boolean => {
  if (!permissions || !projectId) {
    return false;
  }

  if (hasGlobalScope(permissions)) {
    return true;
  }

  return permissions.visibleProjectIds.includes(projectId);
};

export const canAccessContract = (permissions: CurrentUserPermissions | null, contractId: string | undefined): boolean => {
  if (!permissions || !contractId) {
    return false;
  }

  if (hasGlobalScope(permissions)) {
    return true;
  }

  return permissions.visibleContractIds.includes(contractId);
};

const canManageProjectPart = (permissions: CurrentUserPermissions | null, contractId: string | undefined, projectId: string | undefined): boolean => {
  if (!permissions) {
    return false;
  }

  if (hasGlobalScope(permissions)) {
    return true;
  }

  if (contractId && permissions.contractManagerOf.includes(contractId)) {
    return true;
  }

  if (projectId && permissions.projectManagerOf.includes(projectId)) {
    return true;
  }

  return false;
};

const hasManagerRole = (permissions: CurrentUserPermissions | null): boolean =>
  Boolean(permissions && (hasGlobalScope(permissions) || permissions.projectManagerOf.length > 0 || permissions.contractManagerOf.length > 0));

export const can = (
  permissions: CurrentUserPermissions | null,
  currentUserId: string | undefined,
  action: UiActionId,
  ctx: UiContext = {},
): boolean => {
  if (!permissions) {
    return false;
  }

  switch (action) {
    case UiAction.nav.projects:
      return hasGlobalScope(permissions) || permissions.visibleProjectIds.length > 0;

    case UiAction.nav.employees:
      return hasManagerRole(permissions);

    case UiAction.nav.myTimesheets:
      return Boolean(currentUserId);

    case UiAction.nav.employeeRoles:
      return permissions.isRoleManager;

    case UiAction.projects.add:
    case UiAction.projects.edit:
    case UiAction.projects.delete:
      return hasGlobalScope(permissions);

    case UiAction.projects.view:
      return canAccessProject(permissions, ctx.projectId);

    case UiAction.contracts.add:
    case UiAction.contracts.edit:
    case UiAction.contracts.delete:
      return hasGlobalScope(permissions);

    case UiAction.contracts.view:
      return canAccessContract(permissions, ctx.contractId);

    case UiAction.contractManagers.view:
    case UiAction.contractManagers.add:
    case UiAction.contractManagers.remove:
      return hasGlobalScope(permissions) || (ctx.projectId !== undefined && permissions.projectManagerOf.includes(ctx.projectId));

    case UiAction.contractEmployees.view:
    case UiAction.contractEmployees.add:
    case UiAction.contractEmployees.update:
    case UiAction.contractEmployees.remove:
      return hasGlobalScope(permissions) || (ctx.contractId !== undefined && permissions.contractManagerOf.includes(ctx.contractId));

    case UiAction.employeePositions.add:
      return hasManagerRole(permissions);

    case UiAction.employees.list:
      return hasManagerRole(permissions);

    case UiAction.employees.view:
      return isOwnEmployee(currentUserId, ctx.employeeId) || hasManagerRole(permissions);

    case UiAction.employees.editType:
      return hasGlobalScope(permissions);

    case UiAction.timesheet.view:
      return isOwnEmployee(currentUserId, ctx.employeeId) || hasManagerRole(permissions);

    case UiAction.timesheet.import:
      return hasGlobalScope(permissions);

    case UiAction.timesheet.edit:
    case UiAction.timesheet.submit:
      return isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.timesheet.approveProject:
    case UiAction.timesheet.returnProject:
      return canManageProjectPart(permissions, ctx.timesheetContractId ?? ctx.contractId, ctx.timesheetProjectId ?? ctx.projectId);

    case UiAction.timesheet.finalApprove:
    case UiAction.timesheet.returnWhole:
    case UiAction.timesheet.unlock:
      return hasGlobalScope(permissions);

    default:
      return false;
  }
};
