import type { Attendance, TimesheetDay } from "./Timesheet";

export const TimesheetLogic = {
  calculateWorkedHours: (attendance: Attendance): number => {
    const toMin = (t: string) => {
      const [h, m] = (t || "").split(":").map(Number);
      return (h || 0) * 60 + (m || 0);
    };

    const start = toMin(attendance.clockIn);
    const end = toMin(attendance.clockOut);

    if (end <= start) return 0;
    const mins = end - start;
    return Math.max(0, mins / 60);
  },

  getDelta: (day: TimesheetDay): number => {
    const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
    const allocated = day.coreHours + Object.values(day.projectHours).reduce((sum, h) => sum + h, 0);
    return Number((allocated - worked).toFixed(2));
  },

  isValidTime: (time: string): boolean => {
    const regex = /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/;
    return regex.test(time);
  },

  formatSmartTime: (value: string): string => {
    const clean = value.replace(/\D/g, "");
    if (!clean) return "";

    let h = 0;
    let m = 0;

    if (clean.length <= 2) {
      h = parseInt(clean);
    } else {
      h = parseInt(clean.slice(0, -2));
      m = parseInt(clean.slice(-2));
    }

    h = Math.min(h, 23);
    m = Math.min(m, 59);

    return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}`;
  },

  getMonthlyTotalForProject: (days: TimesheetDay[], projectId: string) => {
    return days.reduce((sum, day) => sum + (day.projectHours[projectId] || 0), 0);
  },
};
