import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export const deleteProject = async (id: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${id}`, {
      method: "DELETE",
      signal,
    }),
  );
};
