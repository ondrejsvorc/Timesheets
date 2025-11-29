import { Constants } from "../../common/Constants";
import type { GetProjectsResponse } from "../ProjectsPage";

export const getProjects = async (): Promise<GetProjectsResponse> => {
  const response = await fetch(`${Constants.apiUrl}/projects`);
  if (!response.ok) throw new Error("Failed to fetch projects.");
  return await response.json();
};

