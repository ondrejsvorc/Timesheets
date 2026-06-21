import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface GetProjectResponse {
  project: {
    id: string;
    name: string;
    registrationNumber: string;
  };
}

export interface ProjectContractItem {
  id: string;
  name: string;
  registrationNumber: string;
  employeeCount: number;
}

export interface GetProjectContractsResponse {
  projectContracts: ProjectContractItem[];
}

export interface CreateProjectContractRequest {
  name: string;
  registrationNumber: string;
}

export interface CreateProjectContractResponse {
  projectContract: ProjectContractItem;
}

export interface UpdateProjectContractRequest {
  name: string;
  registrationNumber: string;
}

export interface UpdateProjectContractResponse {
  projectContract: ProjectContractItem;
}

export interface ProjectManagerItem {
  projectId: string;
  employeeId: string;
  employeePersonalNumber: string;
  employeeFullName: string;
}

export interface GetProjectManagersResponse {
  managers: ProjectManagerItem[];
}
export interface ProjectContractManagerItem {
  contractId: string;
  employeeId: string;
  contractRegistrationNumber: string;
  employeePersonalNumber: string;
  employeeFullName: string;
}

export interface GetProjectContractsManagersResponse {
  managers: ProjectContractManagerItem[];
}

export const getProject = (projectId: string): Promise<GetProjectResponse> => {
  return withDelay("fast", () => {
    return customFetch<GetProjectResponse>(`${ApiUrl}/projects/${projectId}`);
  });
};

export const getProjectContracts = (projectId: string): Promise<GetProjectContractsResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetProjectContractsResponse>(`${ApiUrl}/projects/${projectId}/contracts`);
  });
};

export const createProjectContract = (projectId: string, request: CreateProjectContractRequest, signal: AbortSignal): Promise<CreateProjectContractResponse> => {
  return withDelay("fast", () => {
    return customFetch<CreateProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
      }),
      signal,
    });
  });
};

export const updateProjectContract = (projectId: string, contractId: string, request: UpdateProjectContractRequest, signal: AbortSignal): Promise<UpdateProjectContractResponse> => {
  return withDelay("fast", () => {
    return customFetch<UpdateProjectContractResponse>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: request.name,
        registrationNumber: request.registrationNumber,
      }),
      signal,
    });
  });
};

export const deleteProjectContract = (projectId: string, contractId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () => {
    return customFetch<void>(`${ApiUrl}/projects/${projectId}/contracts/${contractId}`, {
      method: "DELETE",
      signal,
    });
  });
};

export const getProjectManagers = (projectId: string): Promise<GetProjectManagersResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetProjectManagersResponse>(`${ApiUrl}/projects/${projectId}/managers`);
  });
};
export const getProjectContractsManagers = (projectId: string): Promise<GetProjectContractsManagersResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetProjectContractsManagersResponse>(`${ApiUrl}/projects/${projectId}/contracts/managers`);
  });
};

export const addProjectManager = (projectId: string, employeeId: string, signal: AbortSignal): Promise<ProjectManagerItem> => {
  return withDelay("fast", () => {
    return customFetch<ProjectManagerItem>(`${ApiUrl}/projects/${projectId}/managers`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ projectId, employeeId }),
      signal,
    });
  });
};

export const removeProjectManager = (projectId: string, employeeId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () => {
    return customFetch<void>(`${ApiUrl}/projects/${projectId}/managers/${employeeId}`, {
      method: "DELETE",
      signal,
    });
  });
};

export const addContractManager = (contractId: string, employeeId: string, signal: AbortSignal): Promise<ProjectContractManagerItem> => {
  return withDelay("fast", () => {
    return customFetch<ProjectContractManagerItem>(`${ApiUrl}/contracts/${contractId}/managers`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ contractId, employeeId }),
      signal,
    });
  });
};

export const removeContractManager = (contractId: string, employeeId: string, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () => {
    return customFetch<void>(`${ApiUrl}/contracts/${contractId}/managers/${employeeId}`, {
      method: "DELETE",
      signal,
    });
  });
};
