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

  const coreFund = TimesheetLogic.calculateWorkloadFund(timesheet, core.workload);
  const coreCurrent = sum((d) => d.coreHours);

  const footerCell = "min-w-0 flex items-center justify-center whitespace-nowrap tabular-nums text-[12px] uppercase tracking-wider";

  return (
    <div className="grid grid-cols-subgrid col-[1/-1] sticky bottom-0 z-20 self-end bg-slate-100 font-bold border-t-2 border-slate-300">
      {/* Den → Přerušení: empty */}
      {[...Array(6)].map((_, i) => (
        <div key={i} />
      ))}

      {/* Odpracováno */}
      <div className={footerCell}>
        {monthlyTotalWorked.toFixed(2)} / {monthlyTotalFund.toFixed(2)}
      </div>

      {/* Noční, STAG */}
      {[...Array(2)].map((_, i) => (
        <div key={`e-${i}`} />
      ))}

      {/* Kmen */}
      <div
        className={cn(
          footerCell,
          coreCurrent > coreFund + 0.01 ? "text-red-600" : "text-blue-800"
        )}
      >
        {coreCurrent.toFixed(2)} / {coreFund.toFixed(2)}
      </div>

      {/* Projekty */}
      {projects.map((p) => {
        const projectFund = TimesheetLogic.calculateWorkloadFund(timesheet, p.workload);
        const projectCurrent = sum((d) => d.projectHours[p.id] ?? 0);
        return (
          <div
            key={p.id}
            className={cn(footerCell, projectCurrent > projectFund + 0.01 ? "text-red-600" : "text-blue-800")}
          >
            {projectCurrent.toFixed(2)} / {projectFund.toFixed(2)}
          </div>
        );
      })}

      {/* Last column: empty, sticky so it stays visible when scrolling */}
      <div className={cn(footerCell, "sticky right-0 z-30 bg-slate-100")} />
    </div>
  );
};
