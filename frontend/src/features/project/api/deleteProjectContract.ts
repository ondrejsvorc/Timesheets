import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export const deleteProjectContract = async (projectId: string, contractId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
      method: "DELETE",
      signal,
    }),
  );
};
