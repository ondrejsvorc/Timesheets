import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export const deleteProjectContract = async (
  projectId: string,
  contractId: string,
  signal: AbortSignal,
): Promise<void> => {
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
      method: "DELETE",
      signal,
    }),
  );
};
