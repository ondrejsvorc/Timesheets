import { ApiUrl, customFetch } from "@/constants/api";

export interface ImportResult {
  fileName: string;
  success: boolean;
  errorMessage: string | null;
  timesheetId: string | null;
  year: number | null;
  month: number | null;
}

export interface TimesheetDetectionResult {
  fileName: string;
  canImport: boolean;
  errorMessage: string | null;
  employeePersonalNumber: number | null;
  employeeName: string | null;
  year: number | null;
  month: number | null;
}

interface ImportTimesheetResponse {
  result: ImportResult;
}

interface DetectTimesheetResponse {
  result: TimesheetDetectionResult;
}

export const detectTimesheetImport = async (employeeId: string, file: File, signal?: AbortSignal): Promise<TimesheetDetectionResult> => {
  const formData = new FormData();
  formData.append("employeeId", employeeId);
  formData.append("file", file);

  const response = await customFetch<DetectTimesheetResponse>(`${ApiUrl}/timesheets/detect`, { method: "POST", body: formData, signal });
  return response.result;
};

export const importTimesheet = async (employeeId: string, file: File, signal?: AbortSignal): Promise<ImportResult> => {
  const formData = new FormData();
  formData.append("employeeId", employeeId);
  formData.append("file", file);

  const response = await customFetch<ImportTimesheetResponse>(`${ApiUrl}/timesheets/`, { method: "POST", body: formData, signal });
  return response.result;
};

