import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { Timesheet } from "../Timesheet";

export const getCombinedTimesheet = (employeeId: string, year: number, month: number) => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return {
    promise: withOptionalDelay("slow", () => customFetch<Timesheet>(`${ApiUrl}/timesheets/combined?${params.toString()}`)),
  };
};
