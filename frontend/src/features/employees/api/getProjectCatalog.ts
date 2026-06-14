import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface ProjectCatalogItem {
  id: string;
  name: string;
}

export interface GetProjectCatalogResponse {
  projects: ProjectCatalogItem[];
}

export const getProjectCatalog = async (): Promise<GetProjectCatalogResponse> => {
  return withDelay("slow", () => customFetch<GetProjectCatalogResponse>(`${ApiUrl}/projects/catalog`));
};
