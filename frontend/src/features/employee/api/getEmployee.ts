import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  fullName: string;
  personalNumber: number;
  email: string;
}

export interface GetEmployeeResponse {
  employee: EmployeeItem;
}

export const getEmployee = (employeeId: string) => {
  return {
    promise: withOptionalDelay("fast", async (): Promise<GetEmployeeResponse> => {
      const employee = await customFetch<EmployeeItem>(`${ApiUrl}/employees/${employeeId}`);
      return { employee };
    }),
  };
};
