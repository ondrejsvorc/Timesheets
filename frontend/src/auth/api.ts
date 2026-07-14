import { BaseUrl, fetchWithAuth } from "@/constants/api";
import type { UserRole } from "./userRole";

export type CurrentUserPermissions = {
  role: UserRole;
  projectManagerOf: string[];
  contractManagerOf: string[];
  employeeOnContractIds: string[];
  visibleProjectIds: string[];
  visibleContractIds: string[];
};

export type CurrentUser = {
  id: string;
  fullName: string;
  employeeType: string | null;
  personalNumber: string;
  titleBefore: string | null;
  titleAfter: string | null;
  permissions: CurrentUserPermissions;
};

export const getCurrentUser = async (): Promise<CurrentUser | null> => {
  const response = await fetchWithAuth(`${BaseUrl}/auth/currentUser`);
  return response.ok ? ((await response.json()) as CurrentUser) : null;
};
