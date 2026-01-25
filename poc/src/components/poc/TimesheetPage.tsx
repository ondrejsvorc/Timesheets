import { useImmer } from "use-immer";
import { FullscreenWrapper } from "./FullscreenWrapper";
import type { Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetTable } from "./TimesheetTable";

const createMockTimesheet = (): Timesheet => {
  const year = 2026;
  const month = 1;

  const days: TimesheetDay[] = Array.from({ length: 31 }, (_, i) => {
    const dayNumber = i + 1;
    const dateObj = new Date(year, month - 1, dayNumber);
    const dayOfWeek = dateObj.getDay(); // 0=Ne, 6=So

    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;
    const isHoliday = dayNumber === 1; // 1.1. Nový rok

    // Simulujeme práci: jen v pracovní dny, které nejsou svátek
    const shouldHaveAttendance = !isWeekend && !isHoliday;

    return {
      date: `${dayNumber.toString().padStart(2, "0")}. 01. ${year}`,
      isWeekend,
      isHoliday,
      attendance: {
        clockIn: shouldHaveAttendance ? "08:00" : "",
        clockOut: shouldHaveAttendance ? "16:30" : "",
        breakStart: shouldHaveAttendance ? "12:00" : "",
        breakEnd: shouldHaveAttendance ? "12:30" : "",
        interruptions: "",
        nightHours: 0,
        schedules: [],
      },
      coreHours: 0,
      projectHours: {
        "p-a": 0,
        "p-b": 0,
      },
    };
  });

  return {
    year,
    month,
    totalWorkload: 1.0,
    core: { workload: 0.6 },
    projects: [
      { id: "p-a", registrationNumber: "22161", name: "Projekt A", workload: 0.3 },
      { id: "p-b", registrationNumber: "XXXXX", name: "Projekt B", workload: 0.1 },
    ],
    days,
  };
};

export const TimesheetPage = () => {
  const [timesheet, updateTimesheet] = useImmer<Timesheet>(createMockTimesheet());

  const handleUpdateDay = (date: string, recipe: (day: TimesheetDay) => void) => {
    updateTimesheet((draft) => {
      const day = draft.days.find((dayInstance) => dayInstance.date === date);
      if (day) recipe(day);
    });
  };

  return (
    <FullscreenWrapper>
      <TimesheetTable timesheet={timesheet} onUpdateDay={handleUpdateDay} />
    </FullscreenWrapper>
  );
};
