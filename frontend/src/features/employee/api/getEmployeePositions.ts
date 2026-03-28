import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface EmployeePositionItem {
  projectId: string;
  projectName: string;
  contractId: string;
  contractName: string;
  positionCode: string;
  position: string;
  workload: number;
  startDate: string;
  endDate: string | null;
}

export interface GetEmployeePositionsResponse {
  employeeId: string;
  positions: EmployeePositionItem[];
}

const toDateOnly = (value: string | null) => value?.slice(0, 10) ?? null;

export const getEmployeePositions = (employeeId: string) => {
  return {
    promise: withOptionalDelay("slow", async (): Promise<GetEmployeePositionsResponse> => {
      const response = await customFetch<GetEmployeePositionsResponse>(`${ApiUrl}/employees/${employeeId}/positions`);

      return {
        employeeId: response.employeeId,
        positions: response.positions.map((position) => ({
          ...position,
          startDate: toDateOnly(position.startDate) ?? position.startDate,
          endDate: toDateOnly(position.endDate),
        })),
      };
    }),
  };
};
