import { useMemo } from "react";
import { cn } from "@/utils/cn";
import type { Timesheet, TimesheetDay } from "../../Timesheet";
import { TimesheetLogic } from "../../TimesheetLogic";
import { TimesheetBody } from "./TimesheetBody";
import { TimesheetFooter } from "./TimesheetFooter";
import { TimesheetHeader } from "./TimesheetHeader";

const createGridTemplate = (projectCount: number) => {
  const base = [
    "minmax(8rem, max-content)" /* Den */,
    "minmax(6rem, max-content)" /* Příchod */,
    "minmax(6rem, max-content)" /* Odchod */,
    "minmax(6rem, max-content)" /* Pauza od */,
    "minmax(6rem, max-content)" /* Pauza do */,
    "minmax(max-content, max-content)" /* Přerušení */,
    "minmax(4rem, max-content)" /* Docházka */,
    "minmax(4rem, max-content)" /* Noční */,
    "minmax(7rem, max-content)" /* STAG */,
    "minmax(5rem, 1fr)" /* Kmen */,
  ];
  const projectColumns = projectCount > 0 ? [`repeat(${projectCount}, minmax(max-content, 1fr))`] : [];
  const control = "minmax(7rem, max-content)"; /* Kontrola */
  const delta = "minmax(7rem, max-content)"; /* Rozdíl */
  return [...base, ...projectColumns, control, delta].join(" ");
};

interface TimesheetGridProps {
  timesheet: Timesheet;
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
  onToggleProjectLock: (projectId: string) => void;
  className?: string;
}

export const TimesheetGrid = ({ timesheet, onUpdateDay, onToggleProjectLock, className }: TimesheetGridProps) => {
  const projectCount = timesheet.projects.length;
  const template = useMemo(() => createGridTemplate(projectCount), [projectCount]);

  return (
    <div className={cn("rounded-md border border-slate-300 overflow-auto max-h-[calc(100vh-100px)] w-full shadow-sm", className)}>
      <div className="grid w-full min-w-max" style={{ gridTemplateColumns: template }}>
        <TimesheetHeader
          projects={timesheet.projects}
          core={timesheet.core}
          onToggleProjectLock={onToggleProjectLock}
          onGenerateMonthly={() => {
            const onUpdateByDate = (date: string, updater: (draftDay: TimesheetDay) => void) => {
              const dayIndex = timesheet.days.findIndex((d) => d.date === date);
              if (dayIndex < 0) return;
              onUpdateDay(dayIndex, updater);
            };
            TimesheetLogic.distributeMonthlyHours(timesheet, onUpdateByDate);
          }}
        />
        <TimesheetBody
          days={timesheet.days}
          projects={timesheet.projects}
          totalWorkload={timesheet.totalWorkload}
          coreWorkload={timesheet.core.workload}
          onUpdateDay={onUpdateDay}
        />
        <TimesheetFooter timesheet={timesheet} />
      </div>
    </div>
  );
};
