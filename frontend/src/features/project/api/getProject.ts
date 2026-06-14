import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface GetProjectResponse {
  project: {
    id: string;
    name: string;
    registrationNumber: string;
  };
}

export const getProject = (id: string) => withDelay("fast", () => customFetch<GetProjectResponse>(`${ApiUrl}/projects/${id}`));
