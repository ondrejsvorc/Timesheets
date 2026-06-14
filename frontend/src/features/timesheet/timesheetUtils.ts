import type { Attendance, TimesheetDay } from "./Timesheet";

export const BUSINESS_TRIP_INTERRUPTION_CODES = new Set(["SCP", "SCS", "SCT", "SCZ", "SCZE", "SCZP", "SCZS"]);
export const CORE_INTERRUPTION_CODES = new Set(["M"]);

export const parseInterruptionCodes = (raw: string): string[] => {
  if (!raw.trim()) {
    return [];
  }

  return raw
    .split(",")
    .map((code) => code.trim().toUpperCase())
    .filter(Boolean);
};

export const toMinutes = (time: string): number => {
  if (!time) {
    return 0;
  }

  const [hours, minutes] = time.split(":").map(Number);
  return (hours || 0) * 60 + (minutes || 0);
};

export const hasBusinessTripInterruption = (attendance: Attendance): boolean => parseInterruptionCodes(attendance.interruptions).some((code) => BUSINESS_TRIP_INTERRUPTION_CODES.has(code));
export const hasCoreOnlyInterruption = (attendance: Attendance): boolean => parseInterruptionCodes(attendance.interruptions).some((code) => CORE_INTERRUPTION_CODES.has(code) || code.startsWith("N"));
export const dayHasBusinessTripInterruption = (day: TimesheetDay): boolean => hasBusinessTripInterruption(day.attendance);
export const dayHasCoreOnlyInterruption = (day: TimesheetDay): boolean => hasCoreOnlyInterruption(day.attendance);
