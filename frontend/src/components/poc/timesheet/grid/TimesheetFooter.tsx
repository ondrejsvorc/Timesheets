import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { cn } from "@/utils/cn";
import type { Timesheet, TimesheetDay } from "../../Timesheet";
import { TimesheetLogic } from "../../TimesheetLogic";

interface TimesheetFooterProps {
  timesheet: Timesheet;
}

export const TimesheetFooter = ({ timesheet }: TimesheetFooterProps) => {
  const { days, projects, core } = timesheet;

  const sum = (fn: (d: TimesheetDay) => number) => days.reduce((acc, d) => acc + fn(d), 0);

  const monthlyTotalWorked = TimesheetLogic.calculateMonthlyTotalWorked(days);
  const monthlyTotalFund = TimesheetLogic.calculateMonthlyFund(timesheet);
  const monthlyControlTotal = sum((d) => TimesheetLogic.calculateControlTotal(d));

  const coreFund = TimesheetLogic.calculateWorkloadFund(timesheet, core.workload);
  const coreCurrent = sum((d) => d.coreHours ?? 0);

  const footerCell = "min-w-0 flex items-center justify-end whitespace-nowrap tabular-nums text-[12px] uppercase tracking-wider px-2";
  const footerCenteredCell = "min-w-0 flex items-center justify-center whitespace-nowrap tabular-nums text-[12px] uppercase tracking-wider px-2";

  return (
    <div className="grid grid-cols-subgrid col-[1/-1] sticky bottom-0 z-20 self-end bg-slate-100 font-bold border-t-2 border-slate-300">
      {/* Den → Přerušení: empty */}
      {[...Array(6)].map((_, i) => (
        <div key={i} />
      ))}

      {/* Odpracováno */}
      <div className={footerCell}>
        <HoursToHumanTooltip hours={monthlyTotalWorked}>
          <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(monthlyTotalWorked)}</span>
        </HoursToHumanTooltip>
        {" / "}
        <HoursToHumanTooltip hours={monthlyTotalFund}>
          <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(monthlyTotalFund)}</span>
        </HoursToHumanTooltip>
      </div>

      {/* Noční, STAG */}
      {[...Array(2)].map((_, i) => (
        <div key={`e-${i}`} />
      ))}

      {/* Kmen */}
      <div className={cn(footerCenteredCell, coreCurrent > coreFund + 0.001 ? "text-red-600" : "text-blue-800")}>
        <HoursToHumanTooltip hours={coreCurrent}>
          <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(coreCurrent)}</span>
        </HoursToHumanTooltip>
        {" / "}
        <HoursToHumanTooltip hours={coreFund}>
          <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(coreFund)}</span>
        </HoursToHumanTooltip>
      </div>

      {/* Projekty */}
      {projects.map((p) => {
        const projectFund = TimesheetLogic.calculateWorkloadFund(timesheet, p.workload);
        const projectCurrent = sum((d) => d.projectHours[p.id] ?? 0);
        return (
          <div key={p.id} className={cn(footerCenteredCell, projectCurrent > projectFund + 0.001 ? "text-red-600" : "text-blue-800")}>
            <HoursToHumanTooltip hours={projectCurrent}>
              <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(projectCurrent)}</span>
            </HoursToHumanTooltip>
            {" / "}
            <HoursToHumanTooltip hours={projectFund}>
              <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(projectFund)}</span>
            </HoursToHumanTooltip>
          </div>
        );
      })}

      {/* Kontrola */}
      <div className={cn(footerCell, "text-slate-700")}>
        <HoursToHumanTooltip hours={monthlyControlTotal}>
          <span className="cursor-help border-b border-dotted border-slate-300">{TimesheetLogic.formatHours(monthlyControlTotal)}</span>
        </HoursToHumanTooltip>
      </div>

      {/* Last column: empty, sticky so it stays visible when scrolling */}
      <div className={cn(footerCell, "sticky right-0 z-30 bg-slate-100")} />
    </div>
  );
};
