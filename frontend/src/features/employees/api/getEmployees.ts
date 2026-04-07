import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  personalNumber: number;
  fullName: string;
  email: string | null;
  isGlobalManager: boolean;
}

export interface GetEmployeesResponse {
  employees: EmployeeItem[];
}

export const getEmployees = () => {
  return {
    promise: withOptionalDelay("slow", () => customFetch<GetEmployeesResponse>(`${ApiUrl}/employees`)),
  };
};
