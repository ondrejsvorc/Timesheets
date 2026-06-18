import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  personalNumber: string;
  fullName: string;
  isGlobalManager: boolean;
}

export interface GetEmployeesResponse {
  employees: EmployeeItem[];
}

export interface ProjectCatalogItem {
  id: string;
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate: string | null;
}

export interface GetProjectCatalogResponse {
  projects: ProjectCatalogItem[];
}

export interface ContractCatalogItem {
  id: string;
  projectId: string;
  name: string;
  registrationNumber: string;
}

export interface GetContractCatalogResponse {
  contracts: ContractCatalogItem[];
}

export interface UpdateEmployeeTypeRequest {
  employeeTypeId: string | null;
}

export const getEmployees = (): Promise<GetEmployeesResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetEmployeesResponse>(`${ApiUrl}/employees`);
  });
};

export const getProjectCatalog = (): Promise<GetProjectCatalogResponse> => {
  return withDelay("slow", () => {
    return customFetch<GetProjectCatalogResponse>(`${ApiUrl}/projects/catalog`);
  });
};

export const getContractCatalog = (projectId?: string): Promise<GetContractCatalogResponse> => {
  if (!projectId) {
    return Promise.resolve({ contracts: [] });
  }

  const params = new URLSearchParams({ projectId });
  return withDelay("slow", () => {
    return customFetch<GetContractCatalogResponse>(`${ApiUrl}/contracts/catalog?${params.toString()}`);
  });
};

export const updateEmployeeType = (employeeId: string, request: UpdateEmployeeTypeRequest, signal: AbortSignal): Promise<void> => {
  return withDelay("fast", () => {
    return customFetch<void>(`${ApiUrl}/employees/${employeeId}/type`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ employeeTypeId: request.employeeTypeId }),
      signal,
    });
  });
};
