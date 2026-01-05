import type { ProjectContractItem } from "./shared/projectContractItem";

export type CreateProjectContractRequest = {
  name: string;
  registrationNumber: string;
};

export type CreateProjectContractResponse = {
  projectContract: ProjectContractItem;
};

// TODO: Pass signal to fetch
export const createProjectContract = async (
  projectId: string,
  request: CreateProjectContractRequest,
  signal: AbortSignal,
): Promise<CreateProjectContractResponse> => {
  const mockResponse: CreateProjectContractResponse = {
    projectContract: {
      id: crypto.randomUUID(),
      name: request.name,
      registrationNumber: request.registrationNumber,
      employeeCount: 0,
    },
  };
  await new Promise((resolve) => setTimeout(resolve, 2000));
  return mockResponse;
};
