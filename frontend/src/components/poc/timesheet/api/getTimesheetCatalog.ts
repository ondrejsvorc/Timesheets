import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface ProjectTimesheetCatalogItem {
  id: string;
  label: string;
}

export interface GetTimesheetCatalogResponse {
  currentStatusId: string;
  projectTimesheets: ProjectTimesheetCatalogItem[];
}

export const getTimesheetCatalog = (employeeId: string, year: number, month: number) => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return {
    promise: withOptionalDelay("fast", () =>
      customFetch<GetTimesheetCatalogResponse>(`${ApiUrl}/timesheets/catalog?${params.toString()}`),
    ),
  };
};
