import type { CurrentUserPermissions } from "./api/getCurrentUserPermissions";

export const hasAnyManagerRole = (permissions: CurrentUserPermissions | null): boolean =>
  Boolean(
    permissions &&
      (permissions.isGlobalManager ||
        permissions.isRoleManager ||
        permissions.projectManagerOf.length > 0 ||
        permissions.contractManagerOf.length > 0),
  );

export const isOwnEmployee = (currentUserId: string | undefined, employeeId: string): boolean =>
  Boolean(currentUserId && currentUserId === employeeId);

export const canManageWholeTimesheet = (permissions: CurrentUserPermissions | null): boolean => Boolean(permissions?.isGlobalManager);

export const canSubmitTimesheet = (currentUserId: string | undefined, employeeId: string): boolean => isOwnEmployee(currentUserId, employeeId);

export const canManageProjectTimesheetPart = (
  permissions: CurrentUserPermissions | null,
  contractId: string | null | undefined,
  projectId: string | null | undefined,
): boolean => {
  if (!permissions) {
    return false;
  }

  if (permissions.isGlobalManager) {
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
