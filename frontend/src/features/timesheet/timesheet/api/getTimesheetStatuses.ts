import { ApiUrl, customFetch, withDelay } from "@/constants/api";

export interface TimesheetStatusItem {
  id: string;
  name: string;
}

export interface GetTimesheetStatusesResponse {
  statuses: TimesheetStatusItem[];
}

export const getTimesheetStatuses = () => withDelay("fast", () => customFetch<GetTimesheetStatusesResponse>(`${ApiUrl}/timesheets/statuses`));
