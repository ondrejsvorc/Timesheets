import { Constants } from "../../common/Constants";

export type CreateProjectRequest = {
};

export type CreateProjectResponse = {
};

export const createProject = async (request: CreateProjectRequest): Promise<CreateProjectResponse> => {
  const response = await fetch(`${Constants.apiUrl}/projects`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error("Failed to create project.");
  return response.json();
}