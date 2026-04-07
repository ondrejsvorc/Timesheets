import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface GetProjectResponse {
  project: {
    id: string;
    name: string;
    registrationNumber: string;
  };
}

export const getProject = (id: string) => {
  return {
    promise: withOptionalDelay("fast", () => customFetch<GetProjectResponse>(`${ApiUrl}/projects/${id}`)),
  };
};
