import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export const deleteProject = async (id: string, options: { force?: boolean }, signal: AbortSignal): Promise<void> => {
  const query = options.force ? "?force=true" : "";
  return withOptionalDelay("fast", () =>
    customFetch<void>(`${ApiUrl}/projects/${id}${query}`, {
      method: "DELETE",
      signal,
    }),
  );
};
