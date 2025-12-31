import type { ProjectItem } from "./shared/projectItem";

export type CreateProjectRequest = {
  name: string;
  registrationNumber: string;
  recipientName: string;
  startDate: string;
  endDate?: string | null;
  description?: string | null;
};

export type CreateProjectResponse = {
  project: ProjectItem;
};

// TODO: Pass signal to fetch
export const createProject = async (request: CreateProjectRequest, signal: AbortSignal): Promise<CreateProjectResponse> => {
  const mockResponse: CreateProjectResponse = {
    project: {
      id: crypto.randomUUID(),
      name: request.name,
      registrationNumber: request.registrationNumber,
      startDate: request.startDate,
      endDate: request.endDate,
      contractCount: 0,
    },
  };
  return mockResponse;
};
