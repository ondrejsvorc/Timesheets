import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export const deleteProject = async (id: string, signal: AbortSignal): Promise<void> => {
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${id}`, {
      method: "DELETE",
      signal,
    }),
  );
};
