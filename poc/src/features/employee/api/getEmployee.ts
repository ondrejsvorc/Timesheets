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

const mockResponse: GetEmployeeResponse = {
  employee: {
    id: "1f4a3b2c-8e6d-4b2a-9f3e-1c2d3e4f5a6b",
    employeeTypeId: "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    fullName: "Jan Novák",
    personalNumber: 2154,
    email: "email@email.cz",
  },
};

export const getEmployee = (_employeeId: string) => {
  return {
    promise: (async (): Promise<GetEmployeeResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 600));
      return mockResponse;
    })(),
  };
};
