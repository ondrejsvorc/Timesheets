import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { cn } from "@/utils/cn";
import type { ProjectDefinition, TimesheetTotals } from "../../Timesheet";
import { formatHours } from "../../timesheetFormat";

interface TimesheetFooterProps {
  readOnly?: boolean;
  tracksAttendance: boolean;
  projects: ProjectDefinition[];
  totals: TimesheetTotals;
}

const Total = ({ value, obligation }: { value: number; obligation: number }) => (
  <>
    <HoursToHumanTooltip hours={value}>
      <span className="cursor-help border-b border-dotted border-slate-300">{formatHours(value)}</span>
    </HoursToHumanTooltip>
    {" / "}
    <HoursToHumanTooltip hours={obligation}>
      <span className="cursor-help border-b border-dotted border-slate-300">{formatHours(obligation)}</span>
    </HoursToHumanTooltip>
  </>
);

export const TimesheetFooter = ({ readOnly = false, tracksAttendance, projects, totals }: TimesheetFooterProps) => {
  const cell = "min-w-0 flex items-center justify-end whitespace-nowrap tabular-nums text-[12px] uppercase tracking-wider px-2";
  const centered = "min-w-0 flex items-center justify-center whitespace-nowrap tabular-nums text-[12px] uppercase tracking-wider px-2";

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1] sticky bottom-0 z-20 self-end bg-slate-100 font-bold border-t-2 border-slate-300", readOnly && "pointer-events-none select-none opacity-80")}>
      <div />
      {tracksAttendance && (
        <>
          <div />
          <div />
          <div />
          <div />
        </>
      )}
      <div />
      {tracksAttendance && (
        <>
          <div className={cell}>
            <Total value={totals.workedHours} obligation={totals.hoursObligation} />
          </div>
          <div />
        </>
      )}
      <div />
      <div className={cn(centered, totals.coreHours > totals.coreHoursObligation ? "text-red-600" : "text-blue-800")}>
        <Total value={totals.coreHours} obligation={totals.coreHoursObligation} />
      </div>
      {projects.map((project) => {
        const total = totals.projects.find((value) => value.projectId === project.id);
        const hours = total?.hours ?? 0;
        const obligation = total?.obligation ?? 0;
        return (
          <div key={project.id} className={cn(centered, hours > obligation ? "text-red-600" : "text-blue-800")}>
            <Total value={hours} obligation={obligation} />
          </div>
        );
      })}
      <div className={cn(cell, "text-slate-700")}>
        <HoursToHumanTooltip hours={totals.allocatedHours}>
          <span className="cursor-help border-b border-dotted border-slate-300">{formatHours(totals.allocatedHours)}</span>
        </HoursToHumanTooltip>
      </div>
      <div className={cn(cell, "sticky right-0 z-30 bg-slate-100")} />
    </div>
  );
};
