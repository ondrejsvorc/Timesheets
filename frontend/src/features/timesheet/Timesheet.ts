export interface TimeRange {
  start: string;
  end: string;
}

export interface Attendance {
  clockIn: string;
  clockOut: string;
  breakStart: string;
  breakEnd: string;
  interruptions: string;
  schedules: TimeRange[];
}

export interface CoreDefinition {
  workload: number;
}

export interface ContractPartDefinition {
  id: string;
  registrationNumber: string;
  name: string;
  position: string;
  workload: number;
  locked: boolean;
  activeDays: boolean[];
}

export interface ContractPartCell {
  hours: number;
  locked: boolean;
}

export interface TimesheetDay {
  date: string;
  attendance: Attendance;
  coreHours: number | null;
  contractPartCells: Record<string, ContractPartCell>;
  isHoliday: boolean;
  isWeekend: boolean;
  attendanceAdjusted?: boolean;
}

export interface Timesheet {
  id: string;
  year: number;
  month: number;
  tracksAttendance: boolean;
  core: CoreDefinition;
  contractParts: ContractPartDefinition[];
  days: TimesheetDay[];
}

export type IssueType = "error" | "warning";

export interface TimesheetIssue {
  code: string;
  type: IssueType;
  message: string;
  day?: number;
  field?: string;
}

export interface TimesheetDayEvaluation {
  day: number;
  workedHours: number;
  nightHours: number;
  allocatedHours: number;
  balance: number;
  hasBusinessTrip: boolean;
  hasCoreOnlyInterruption: boolean;
  hasProportionalInterruption: boolean;
}

export interface ContractPartTotal {
  contractEmployeeId: string;
  hours: number;
  obligation: number;
}

export interface TimesheetTotals {
  workedHours: number;
  hoursObligation: number;
  allocatedHours: number;
  coreHours: number;
  coreHoursObligation: number;
  contractParts: ContractPartTotal[];
}

export interface TimesheetEvaluation {
  hasErrors: boolean;
  issues: TimesheetIssue[];
  days: TimesheetDayEvaluation[];
  totals: TimesheetTotals;
}

export interface TimesheetData {
  timesheet: Timesheet;
  evaluation: TimesheetEvaluation;
}
