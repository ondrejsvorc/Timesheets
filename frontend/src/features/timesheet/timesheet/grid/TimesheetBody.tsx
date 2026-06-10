import { useEffect, useState } from "react";
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

export const TimesheetBody = ({ readOnly = false, days, projects, totalWorkload, coreWorkload, onUpdateDay }: TimesheetBodyProps) => {
  // Tuned for month-sized datasets (typically 28-31 rows):
  // - render enough rows immediately so the table feels "ready"
  // - then fill the rest in small chunks to keep UI smooth
  const INITIAL_ROWS = 16;
  const CHUNK_SIZE = 6;
  const CHUNK_DELAY_MS = 1;
  const [renderedCount, setRenderedCount] = useState(() => Math.min(INITIAL_ROWS, days.length));

  useEffect(() => {
    setRenderedCount(Math.min(INITIAL_ROWS, days.length));
  }, [days.length]);

  useEffect(() => {
    if (renderedCount >= days.length) return;

    const timerId = window.setTimeout(() => {
      setRenderedCount((current) => Math.min(days.length, current + CHUNK_SIZE));
    }, CHUNK_DELAY_MS);

    return () => window.clearTimeout(timerId);
  }, [renderedCount, days.length]);

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1]", readOnly && "pointer-events-none select-none opacity-80")}>
      {days.slice(0, renderedCount).map((day, index) => (
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
};
