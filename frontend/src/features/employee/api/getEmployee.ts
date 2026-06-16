import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  fullName: string;
  personalNumber: string;
}

export interface GetEmployeeResponse {
  employee: EmployeeItem;
}

export const getEmployee = (employeeId: string) =>
  withDelay("fast", async (): Promise<GetEmployeeResponse> => {
    const employee = await customFetch<EmployeeItem>(`${ApiUrl}/employees/${employeeId}`);
    return { employee };
  });
