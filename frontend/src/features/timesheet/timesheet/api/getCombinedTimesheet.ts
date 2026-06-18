import { ApiUrl, customFetch, withDelay } from "@/constants/api";
import type { TimeRange, Timesheet, TimesheetData } from "../../Timesheet";
import { reviewTimesheet } from "./reviewTimesheet";

interface CompactProjectDefinition {
  id: string;
  registrationNumber: string;
  name: string;
  position: string;
  workload: number;
  locked: boolean;
  activeDays: boolean[];
}

interface CompactDayItem {
  day: number;
  work: [number | null, number | null];
  break: [number | null, number | null];
  coreHours: number;
  projectHours: number[];
  isHoliday: boolean;
  isWeekend: boolean;
  note?: string | null;
  schedules?: Array<[number, number]> | null;
}

interface GetCombinedTimesheetResponse {
  id: string;
  year: number;
  month: number;
  coreWorkload: number;
  tracksAttendance: boolean;
  projects: CompactProjectDefinition[];
  days: CompactDayItem[];
}

const pad2 = (value: number) => value.toString().padStart(2, "0");
const minutesToTime = (value: number | null | undefined) => {
  if (value == null || value < 0) return "";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  return `${pad2(hours)}:${pad2(minutes)}`;
};
const minutesToDate = (year: number, month: number, day: number) => {
  return `${pad2(day)}. ${pad2(month)}. ${year}`;
};
const mapSchedules = (schedules: Array<[number, number]> | null | undefined): TimeRange[] => {
  if (!schedules?.length) return [];
  return schedules.map(([start, end]) => ({
    start: minutesToTime(start),
    end: minutesToTime(end),
  }));
};

const mapToTimesheet = (response: GetCombinedTimesheetResponse): Timesheet => {
  const projects = response.projects.map((project) => ({
    id: project.id,
    registrationNumber: project.registrationNumber,
    name: project.name,
    position: project.position,
    workload: project.workload,
    locked: project.locked,
    activeDays: project.activeDays,
  }));

  const days = response.days.map((day) => {
    const projectHours = projects.reduce<Record<string, number>>((acc, project, index) => {
      acc[project.id] = day.projectHours[index] ?? 0;
      return acc;
    }, {});

    return {
      date: minutesToDate(response.year, response.month, day.day),
      attendance: {
        clockIn: minutesToTime(day.work?.[0]),
        clockOut: minutesToTime(day.work?.[1]),
        breakStart: minutesToTime(day.break?.[0]),
        breakEnd: minutesToTime(day.break?.[1]),
        interruptions: day.note ?? "",
        schedules: mapSchedules(day.schedules),
      },
      coreHours: day.coreHours > 0 ? day.coreHours : null,
      projectHours,
      isHoliday: day.isHoliday,
      isWeekend: day.isWeekend,
    };
  });

  return {
    id: response.id,
    year: response.year,
    month: response.month,
    tracksAttendance: response.tracksAttendance,
    core: { workload: response.coreWorkload },
    projects,
    days,
  };
};

export const getCombinedTimesheet = (employeeId: string, year: number, month: number): Promise<TimesheetData> => {
  const params = new URLSearchParams({
    employeeId,
    year: String(year),
    month: String(month),
  });

  return withDelay("slowest", async () => {
    const response = await customFetch<GetCombinedTimesheetResponse>(`${ApiUrl}/timesheets/combined?${params.toString()}`);
    const timesheet = mapToTimesheet(response);
    return { timesheet, evaluation: await reviewTimesheet(timesheet) };
  });
};
