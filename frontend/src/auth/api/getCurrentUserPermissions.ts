import { BaseUrl, customFetch, withDelay } from "@/constants/api";
import type { UserRole } from "../userRole";

export interface CurrentUserPermissions {
  role: UserRole;
  projectManagerOf: string[];
  contractManagerOf: string[];
  employeeOnContractIds: string[];
  visibleProjectIds: string[];
  visibleContractIds: string[];
}

interface GetCurrentUserPermissionsResponse {
  role: UserRole;
  projectManagerOf: string[];
  contractManagerOf: string[];
  employeeOnContractIds: string[];
  visibleProjectIds: string[];
  visibleContractIds: string[];
}

export const getCurrentUserPermissions = () =>
  withDelay("fast", () =>
    customFetch<GetCurrentUserPermissionsResponse>(`${BaseUrl}/auth/currentUserPermissions`, {
      credentials: "include",
    }),
  );
