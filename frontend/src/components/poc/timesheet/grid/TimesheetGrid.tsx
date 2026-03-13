import { useMemo } from "react";
import { cn } from "@/utils/cn";
import type { Timesheet, TimesheetDay } from "../../Timesheet";
import { TimesheetBody } from "./TimesheetBody";
import { TimesheetFooter } from "./TimesheetFooter";
import { TimesheetHeader } from "./TimesheetHeader";

const createGridTemplate = (projectCount: number) => {
  const base = [
    "minmax(8rem, max-content)",   /* Den */
    "minmax(7rem, max-content)", /* Příchod */
    "minmax(7rem, max-content)", /* Odchod */
    "minmax(7rem, max-content)", /* Pauza od */
    "minmax(7rem, max-content)", /* Pauza do */
    "minmax(max-content, max-content)",   /* Přerušení */
    "minmax(7rem, max-content)",   /* Odpracováno */
    "minmax(7rem, max-content)",   /* Noční */
    "minmax(7rem, max-content)",   /* STAG */
    "minmax(5rem, 1fr)", /* Kmen */
  ];
  const projectCols = projectCount > 0 ? [`repeat(${projectCount}, minmax(max-content, 1fr))`] : [];
  const last = "minmax(5rem, max-content)"; /* Generovat */
  return [...base, ...projectCols, last].join(" ");
};

interface TimesheetGridProps {
  timesheet: Timesheet;
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
  className?: string;
}

export const TimesheetGrid = ({ timesheet, onUpdateDay, className }: TimesheetGridProps) => {
  const projectCount = timesheet.projects.length;
  const template = useMemo(() => createGridTemplate(projectCount), [projectCount]);

  return (
    <div className={cn("rounded-md border border-slate-300 overflow-auto max-h-[calc(100vh-100px)] w-full shadow-sm", className)}>
      <div className="grid w-full min-w-max" style={{ gridTemplateColumns: template }}>
        <TimesheetHeader projects={timesheet.projects} core={timesheet.core} />
        <TimesheetBody days={timesheet.days} projects={timesheet.projects} onUpdateDay={onUpdateDay} />
        <TimesheetFooter timesheet={timesheet} />
      </div>
    </div>
  );
};
