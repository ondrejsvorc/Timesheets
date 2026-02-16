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
  nightHours: number;
  schedules: TimeRange[];
}

export interface CoreDefinition {
  workload: number;
}

export interface ProjectDefinition {
  id: string;
  registrationNumber: string;
  name: string;
  workload: number;
}

export interface TimesheetDay {
  date: string;
  attendance: Attendance;
  coreHours: number;
  projectHours: Record<string, number>;
  isHoliday: boolean;
  isWeekend: boolean;
}

export interface Timesheet {
  year: number;
  month: number;
  totalWorkload: number;
  core: CoreDefinition;
  projects: ProjectDefinition[];
  days: TimesheetDay[];
}
