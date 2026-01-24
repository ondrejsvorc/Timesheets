import { useImmer } from "use-immer";
import { FullscreenWrapper } from "./FullscreenWrapper";
import type { Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetTable } from "./TimesheetTable";

const createMockTimesheet = (): Timesheet => {
  const days: TimesheetDay[] = Array.from({ length: 31 }, (_, i) => ({
    date: `${(i + 1).toString().padStart(2, "0")}. 01. 2026`,
    attendance: {
      clockIn: "",
      clockOut: "",
      breakStart: "",
      breakEnd: "",
      interruptions: "",
      schedules: [],
    },
    coreHours: 0,
    projectHours: {
      "p-a": 0,
      "p-b": 0,
    },
  }));

  return {
    year: 2026,
    month: 1,
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
