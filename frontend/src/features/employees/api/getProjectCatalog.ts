import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface ProjectCatalogItem {
  id: string;
  name: string;
}

export interface GetProjectCatalogResponse {
  projects: ProjectCatalogItem[];
}

export const getProjectCatalog = async (): Promise<GetProjectCatalogResponse> => {
  return withOptionalDelay("slow", () => customFetch<GetProjectCatalogResponse>(`${ApiUrl}/projects/catalog`));
};
