import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export type UpdateEmployeeGlobalManagerRequest = {
  isGlobalManager: boolean;
};

export const updateEmployeeGlobalManager = async (employeeId: string, request: UpdateEmployeeGlobalManagerRequest, signal: AbortSignal): Promise<void> =>
  withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/employees/${employeeId}/global-manager`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    }),
  );
