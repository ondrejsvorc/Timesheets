import { BaseUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface CurrentUserPermissions {
  isRoleManager: boolean;
  isGlobalManager: boolean;
  projectManagerOf: string[];
  contractManagerOf: string[];
}

interface GetCurrentUserPermissionsResponse {
  isRoleManager: boolean;
  isGlobalManager: boolean;
  projectManagerOf: string[];
  contractManagerOf: string[];
}

export const getCurrentUserPermissions = () =>
  withOptionalDelay("fast", () =>
    customFetch<GetCurrentUserPermissionsResponse>(`${BaseUrl}/auth/currentUserPermissions`, {
      credentials: "include",
    }),
  );
