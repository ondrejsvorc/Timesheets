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

export const getEmployees = () => withDelay("slow", () => customFetch<GetEmployeesResponse>(`${ApiUrl}/employees`));
