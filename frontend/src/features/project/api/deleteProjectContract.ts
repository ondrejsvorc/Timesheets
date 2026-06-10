import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export const deleteProjectContract = async (
  projectId: string,
  contractId: string,
  options: { force?: boolean },
  signal: AbortSignal,
): Promise<void> => {
  const query = options.force ? "?force=true" : "";
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}${query}`, {
      method: "DELETE",
      signal,
    }),
  );
};
