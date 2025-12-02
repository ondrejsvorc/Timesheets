export interface GetEmployeesResponse {
  employees: EmployeeItem[];
}

export interface EmployeeItem {
  id: string;
  employeeTypeId: string;
  personalNumber: number;
  fullName: string;
  email: string | null;
  isGlobalManager: boolean;
}

const mockData: GetEmployeesResponse = {
  employees: [
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4070",
      employeeTypeId: "1",
      personalNumber: 2154,
      fullName: "Jan Novák",
      email: "email@email.cz",
      isGlobalManager: false,
    },
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4071",
      employeeTypeId: "2",
      personalNumber: 2721,
      fullName: "David Dvořák",
      email: "email@email.cz",
      isGlobalManager: true,
    },
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4072",
      employeeTypeId: "3",
      personalNumber: 2987,
      fullName: "Petra Malá",
      email: "email@email.cz",
      isGlobalManager: false,
    },
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4073",
      employeeTypeId: "4",
      personalNumber: 2647,
      fullName: "Karel Nový",
      email: "email@email.cz",
      isGlobalManager: false,
    },
    {
      id: "4efc77cd-0479-4b57-b2c2-11783faf4074",
      employeeTypeId: "5",
      personalNumber: 2812,
      fullName: "Marie Štěpánková",
      email: "email@email.cz",
      isGlobalManager: false,
    },
  ],
};

export const getEmployees = async (): Promise<GetEmployeesResponse> => {
  return mockData;
};