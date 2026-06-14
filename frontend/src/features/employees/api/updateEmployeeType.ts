import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export type UpdateEmployeeTypeRequest = {
  employeeTypeId: string | null;
};

export const updateEmployeeType = async (employeeId: string, request: UpdateEmployeeTypeRequest, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/employees/${employeeId}/type`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ employeeTypeId: request.employeeTypeId }),
      signal,
    }),
  );
};
