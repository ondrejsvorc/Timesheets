import { cn } from "@/utils/common";
import type { ProjectDefinition as Project, TimesheetDay, TimesheetEvaluation } from "../../Timesheet";
import { TimesheetDay as TimesheetDayRow } from "./TimesheetDay";

interface TimesheetBodyProps {
  readOnly?: boolean;
  tracksAttendance: boolean;
  days: TimesheetDay[];
  projects: Project[];
  evaluation: TimesheetEvaluation;
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
  onAllocate: (day?: number) => Promise<void>;
}

export const TimesheetBody = ({ readOnly = false, tracksAttendance, days, projects, evaluation, onUpdateDay, onAllocate }: TimesheetBodyProps) => (
  <div className={cn("grid grid-cols-subgrid col-[1/-1]", readOnly && "pointer-events-none select-none opacity-80")}>
    {days.map((day, index) => (
      <TimesheetDayRow
        key={day.date}
        tracksAttendance={tracksAttendance}
        day={day}
        dayIndex={index}
        projects={projects}
        evaluation={evaluation.days[index]}
        issues={evaluation.issues.filter((issue) => issue.day === index + 1)}
        onUpdateDay={onUpdateDay}
        onAllocate={onAllocate}
      />
    ))}
  </div>
);
