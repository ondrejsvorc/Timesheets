import { ApiUrl } from "@/constants/api";

export interface DetectedFile {
  fileName: string;
  employeeId: string | null;
  employeePersonalNumber: number | null;
  employeeName: string | null;
  year: number | null;
  month: number | null;
  canImport: boolean;
  errorMessage: string | null;
}

export interface FileSelection {
  fileName: string;
  employeeId: string;
  contractId: string;
  year: number;
  month: number;
}

export interface ImportResult {
  fileName: string;
  success: boolean;
  errorMessage: string | null;
  timesheetId: string | null;
}

export interface ConfirmImportResponse {
  results: ImportResult[];
}

export const confirmTimesheetImport = async (
  files: File[],
  selections: FileSelection[],
): Promise<ConfirmImportResponse> => {
  const formData = new FormData();
  files.forEach((file) => {
    formData.append("files", file);
  });
  formData.append("selectionsJson", JSON.stringify(selections));

  const response = await fetch(`${ApiUrl}/timesheets/import`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error("Failed to import timesheets");
  }

  return response.json();
};

