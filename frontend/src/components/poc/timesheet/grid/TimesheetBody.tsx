import type { ProjectDefinition as Project, TimesheetDay } from "../../Timesheet";
import { DayValidationsProvider } from "./DayValidationsContext";
import { TimesheetDay as TimesheetDayRow } from "./TimesheetDay";

interface TimesheetBodyProps {
  days: TimesheetDay[];
  projects: Project[];
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
}

export const TimesheetBody = ({ days, projects, onUpdateDay }: TimesheetBodyProps) => {
  return (
    <div className="grid grid-cols-subgrid col-[1/-1]">
      {days.map((day, index) => (
        <DayValidationsProvider
          key={day.date}
          day={day}
          previousDay={index > 0 ? days[index - 1] : undefined}
        >
          <TimesheetDayRow
            day={day}
            dayIndex={index}
            projects={projects}
            onUpdateDay={onUpdateDay}
          />
        </DayValidationsProvider>
      ))}
    </div>
  );
};
