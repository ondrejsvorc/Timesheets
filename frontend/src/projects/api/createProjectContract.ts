import { Constants } from "../../common/Constants";

export type CreateProjectContractRequest = {
  name: string;
  registrationNumber: string | null;
  startDate: string;
  endDate: string | null;
  description: string | null;
};

export type CreateProjectContractResponse = {
  id: string;
};

export const createProjectContract = async (
  projectId: string,
  request: CreateProjectContractRequest
): Promise<CreateProjectContractResponse> => {
  const response = await fetch(`${Constants.apiUrl}/projects/${projectId}/contracts`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error("Failed to create project contract.");
  return await response.json();
};

