import { cn } from "@/utils/cn";
import type { ProjectDefinition as Project, TimesheetDay } from "../../Timesheet";
import { TimesheetDay as TimesheetDayRow } from "./TimesheetDay";

interface TimesheetBodyProps {
  readOnly?: boolean;
  days: TimesheetDay[];
  projects: Project[];
  totalWorkload: number;
  coreWorkload: number;
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
}

export const TimesheetBody = ({ readOnly = false, days, projects, totalWorkload, coreWorkload, onUpdateDay }: TimesheetBodyProps) => (
  <div className={cn("grid grid-cols-subgrid col-[1/-1]", readOnly && "pointer-events-none select-none opacity-80")}>
    {days.map((day, index) => (
      <TimesheetDayRow
        key={day.date}
        day={day}
        previousDay={index > 0 ? days[index - 1] : undefined}
        dayIndex={index}
        projects={projects}
        totalWorkload={totalWorkload}
        coreWorkload={coreWorkload}
        onUpdateDay={onUpdateDay}
      />
    ))}
  </div>
);
