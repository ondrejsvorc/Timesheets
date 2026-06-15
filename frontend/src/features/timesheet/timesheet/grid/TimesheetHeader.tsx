import { Copy, Lock, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Texts } from "@/constants/texts";
import type { CoreDefinition, ProjectDefinition } from "../../Timesheet";

interface TimesheetHeaderProps {
  readOnly?: boolean;
  tracksAttendance: boolean;
  projects: ProjectDefinition[];
  core: CoreDefinition;
  onGenerateMonthly: () => void | Promise<void>;
  onCopyProjectColumn: (projectId: string) => void | Promise<void>;
}

const formatWorkloadPercent = (workload: number) => {
  return Number((workload * 100).toFixed(2))
    .toString()
    .replace(".", ",");
};

export const TimesheetHeader = ({ readOnly = false, tracksAttendance, projects, core, onGenerateMonthly, onCopyProjectColumn }: TimesheetHeaderProps) => {
  return (
    <div className="relative z-20 grid grid-cols-subgrid col-[1/-1] sticky top-0 self-start bg-slate-100 border-b border-slate-300">
      <div className="sticky left-0 z-40 bg-slate-100 border-r border-slate-300 h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.day}</div>
      {tracksAttendance && (
        <>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.clockIn}</div>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.clockOut}</div>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.breakStart}</div>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.breakEnd}</div>
        </>
      )}
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.interruption}</div>
      {tracksAttendance && (
        <>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.attendance}</div>
          <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.nightWork}</div>
        </>
      )}
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">STAG</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Kmen ({formatWorkloadPercent(core.workload)}%)</div>
      {projects.map((project) => (
        <div key={project.id} className="h-10 px-2 flex items-center justify-center gap-1 text-center font-medium whitespace-nowrap min-w-0">
          <Tooltip delayDuration={120}>
            <TooltipTrigger asChild>
              <span className="cursor-help border-b border-dotted border-slate-400">{project.registrationNumber || Texts.noContractNumber}</span>
            </TooltipTrigger>
            <TooltipContent side="top">
              {project.name} · {formatWorkloadPercent(project.workload)} %
            </TooltipContent>
          </Tooltip>
          {project.locked && <Lock className="h-3.5 w-3.5 text-slate-500" aria-label={Texts.lockContractColumn} />}
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 text-slate-500 hover:text-slate-700 hover:bg-slate-200 transition-all active:scale-90"
            onClick={() => void onCopyProjectColumn(project.id)}
            title={Texts.copyProjectColumn}
          >
            <Copy className="h-3.5 w-3.5" />
          </Button>
        </div>
      ))}
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">{Texts.control}</div>
      <div className="sticky right-0 z-40 bg-slate-100 border-r border-slate-300 h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
        <div className="flex items-center justify-center gap-1">
          <span>{Texts.difference}</span>
          {!readOnly && (
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 text-blue-500 hover:text-blue-600 hover:bg-blue-50 transition-all active:scale-90"
              onClick={() => void onGenerateMonthly()}
              title={Texts.fillMissingHoursInTimesheet}
            >
              <Sparkles className="h-3.5 w-3.5 fill-blue-100" />
            </Button>
          )}
        </div>
      </div>
    </div>
  );
};
