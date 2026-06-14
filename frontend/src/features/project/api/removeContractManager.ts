import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export const removeContractManager = async (contractId: string, employeeId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/contracts/${contractId}/managers/${employeeId}`, {
      method: "DELETE",
      signal,
    }),
  );
};
