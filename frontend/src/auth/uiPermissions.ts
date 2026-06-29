import type { CurrentUserPermissions } from "./api/getCurrentUserPermissions";
import { isAtLeast, UserRole } from "./userRole";

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
  projectManagers: {
    view: "projectManagers.view",
    add: "projectManagers.add",
    remove: "projectManagers.remove",
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
    listContract: "timesheet.listContract",
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

const isGlobalManager = (permissions: CurrentUserPermissions) => isAtLeast(permissions.role, UserRole.GlobalManager);
const isManager = (permissions: CurrentUserPermissions) => isAtLeast(permissions.role, UserRole.ContractManager);
const isOwnEmployee = (currentUserId: string | undefined, employeeId: string | undefined) => Boolean(currentUserId && employeeId && currentUserId === employeeId);

const canSeeProject = (permissions: CurrentUserPermissions, projectId?: string) =>
  isManager(permissions) && (isGlobalManager(permissions) || Boolean(projectId && permissions.visibleProjectIds.includes(projectId)));

const canSeeContract = (permissions: CurrentUserPermissions, contractId?: string) =>
  isManager(permissions) && (isGlobalManager(permissions) || Boolean(contractId && permissions.visibleContractIds.includes(contractId)));

const canManageProject = (permissions: CurrentUserPermissions, projectId?: string) => isGlobalManager(permissions) || Boolean(projectId && permissions.projectManagerOf.includes(projectId));

const canManageContractScope = (permissions: CurrentUserPermissions, ctx: UiContext) => {
  if (isGlobalManager(permissions)) {
    return true;
  }

  if (ctx.contractId && permissions.contractManagerOf.includes(ctx.contractId)) {
    return true;
  }

  if (ctx.projectId && permissions.projectManagerOf.includes(ctx.projectId)) {
    return true;
  }

  return false;
};

export const can = (permissions: CurrentUserPermissions | null, currentUserId: string | undefined, action: UiActionId, ctx: UiContext = {}): boolean => {
  if (!permissions) {
    return false;
  }

  switch (action) {
    case UiAction.nav.projects:
      return isManager(permissions);
    case UiAction.nav.employees:
      return isManager(permissions);
    case UiAction.nav.myTimesheets:
      return Boolean(currentUserId);
    case UiAction.nav.employeeRoles:
      return permissions.role === UserRole.Admin;

    case UiAction.projects.add:
    case UiAction.projects.delete:
      return isGlobalManager(permissions);

    case UiAction.timesheet.import:
      return isGlobalManager(permissions) || isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.projects.edit:
      return canManageProject(permissions, ctx.projectId);
    case UiAction.contracts.add:
      return canManageProject(permissions, ctx.projectId);
    case UiAction.contracts.edit:
    case UiAction.contracts.delete:
      return canManageContractScope(permissions, ctx);

    case UiAction.projects.view:
      return canSeeProject(permissions, ctx.projectId);
    case UiAction.contracts.view:
    case UiAction.contractEmployees.view:
      return canSeeContract(permissions, ctx.contractId);

    case UiAction.projectManagers.view:
    case UiAction.projectManagers.add:
    case UiAction.projectManagers.remove:
    case UiAction.contractManagers.view:
    case UiAction.contractManagers.add:
    case UiAction.contractManagers.remove:
      return isGlobalManager(permissions) || Boolean(ctx.projectId && permissions.projectManagerOf.includes(ctx.projectId));

    case UiAction.contractEmployees.add:
    case UiAction.contractEmployees.update:
    case UiAction.contractEmployees.remove:
      return canManageContractScope(permissions, ctx);

    case UiAction.employeePositions.add:
    case UiAction.employees.list:
      return isManager(permissions);

    case UiAction.employees.view:
    case UiAction.timesheet.view:
      return isOwnEmployee(currentUserId, ctx.employeeId) || isManager(permissions);

    case UiAction.timesheet.edit:
      return isGlobalManager(permissions) || isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.timesheet.submit:
      return isGlobalManager(permissions) || isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.timesheet.returnWhole:
    case UiAction.timesheet.unlock:
      return isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.timesheet.finalApprove:
      return isGlobalManager(permissions) || isOwnEmployee(currentUserId, ctx.employeeId);

    case UiAction.timesheet.approveProject:
    case UiAction.timesheet.returnProject:
    case UiAction.timesheet.listContract:
      return canManageContractScope(permissions, {
        contractId: ctx.timesheetContractId ?? ctx.contractId,
        projectId: ctx.timesheetProjectId ?? ctx.projectId,
      });

    default:
      return false;
  }
};
