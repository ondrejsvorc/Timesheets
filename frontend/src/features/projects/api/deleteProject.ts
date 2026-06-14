import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export const deleteProject = async (id: string, options: { force?: boolean }, signal: AbortSignal): Promise<void> => {
  const query = options.force ? "?force=true" : "";
  return withDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${id}${query}`, {
      method: "DELETE",
      signal,
    }),
  );
};
