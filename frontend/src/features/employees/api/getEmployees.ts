export interface EmployeeItem {
  id: string;
  employeeTypeId: string | null;
  personalNumber: number | null;
  fullName: string;
  email: string | null;
  isGlobalManager: boolean;
}

export interface GetEmployeesResponse {
  employees: EmployeeItem[];
}

const mockResponse: GetEmployeesResponse = {
  employees: [
    {
      id: "1f4a3b2c-8e6d-4b2a-9f3e-1c2d3e4f5a6b",
      employeeTypeId: "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
      personalNumber: 10234,
      fullName: "Jan Novák",
      email: "jan.novak@ujep.cz",
      isGlobalManager: true,
    },
    {
      id: "2a5c6e7f-9b1d-4e3a-8c2f-6b5d4a3e2c1f",
      employeeTypeId: "b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e",
      personalNumber: 10456,
      fullName: "Petra Svobodová",
      email: "petra.svobodova@ujep.cz",
      isGlobalManager: false,
    },
    {
      id: "3e7a9c1d-4b5f-4c8e-9a2d-7f6e5b4c3a2d",
      employeeTypeId: "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
      personalNumber: 10789,
      fullName: "Martin Dvořák",
      email: "martin.dvorak@ujep.cz",
      isGlobalManager: false,
    },
    {
      id: "4b6d8f1a-2c3e-4a9b-8d7e-5c4a3f2e1d0b",
      employeeTypeId: "d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a",
      personalNumber: 11012,
      fullName: "Lucie Černá",
      email: "lucie.cerna@ujep.cz",
      isGlobalManager: false,
    },
    {
      id: "5c1e3a7b-9d4f-4e2a-8b6c-0d9f8e7a6b5c",
      employeeTypeId: "e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b",
      personalNumber: 11345,
      fullName: "Tomáš Procházka",
      email: "tomas.prochazka@ujep.cz",
      isGlobalManager: false,
    },
  ],
};

export const getEmployees = () => {
  return {
    promise: (async (): Promise<GetEmployeesResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      return mockResponse;
    })(),
  };
};
