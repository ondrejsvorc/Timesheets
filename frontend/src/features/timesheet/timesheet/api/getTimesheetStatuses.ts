import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";

export interface TimesheetStatusItem {
  id: string;
  name: string;
}

export interface GetTimesheetStatusesResponse {
  statuses: TimesheetStatusItem[];
}

export const getTimesheetStatuses = () => ({
  promise: withOptionalDelay("fast", () => customFetch<GetTimesheetStatusesResponse>(`${ApiUrl}/timesheets/statuses`)),
});
