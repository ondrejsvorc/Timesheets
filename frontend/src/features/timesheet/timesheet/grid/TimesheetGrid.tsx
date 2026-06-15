import { useMemo } from "react";
import { toast } from "sonner";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import type { Timesheet, TimesheetDay, TimesheetEvaluation } from "../../Timesheet";
import { formatHours } from "../../timesheetFormat";
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
  evaluation: TimesheetEvaluation;
  readOnly?: boolean;
  onUpdateDay: (index: number, updater: (day: TimesheetDay) => void) => void;
  onToggleProjectLock: (projectId: string) => void;
  onAllocate: (day?: number) => Promise<void>;
  className?: string;
}

export const TimesheetGrid = ({ timesheet, evaluation, readOnly = false, onUpdateDay, onToggleProjectLock, onAllocate, className }: TimesheetGridProps) => {
  const projectCount = timesheet.projects.length;
  const template = useMemo(() => createGridTemplate(projectCount), [projectCount]);

  const copyProjectColumn = async (projectId: string) => {
    const lines = timesheet.days.map((day) => {
      const hours = day.projectHours[projectId] ?? 0;
      return formatHours(hours);
    });

    try {
      await navigator.clipboard.writeText(lines.join("\n"));
      toast.success(Texts.copyProjectColumnSuccess);
    } catch {
      toast.error(Texts.actionFailed);
    }
  };

  return (
    <div className={cn("rounded-md border border-slate-300 overflow-auto max-h-[calc(100vh-100px)] w-full shadow-sm", readOnly && "bg-muted/40", className)}>
      <div className="relative grid w-full min-w-max" style={{ gridTemplateColumns: template }}>
        {readOnly && <div className="pointer-events-none absolute inset-0 z-[5] bg-muted/20" aria-hidden />}
        <TimesheetHeader
          readOnly={readOnly}
          projects={timesheet.projects}
          core={timesheet.core}
          onToggleProjectLock={onToggleProjectLock}
          onCopyProjectColumn={copyProjectColumn}
          onGenerateMonthly={() => onAllocate()}
        />
        <TimesheetBody readOnly={readOnly} days={timesheet.days} projects={timesheet.projects} evaluation={evaluation} onUpdateDay={onUpdateDay} onAllocate={onAllocate} />
        <TimesheetFooter readOnly={readOnly} projects={timesheet.projects} totals={evaluation.totals} />
      </div>
    </div>
  );
};
