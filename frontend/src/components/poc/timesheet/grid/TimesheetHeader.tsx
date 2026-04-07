import { Sparkles } from "lucide-react";
import { ProjectLockToggleButton } from "@/components/shared/buttons/ActionButtons";
import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { CoreDefinition, ProjectDefinition } from "../../Timesheet";

interface TimesheetHeaderProps {
  projects: ProjectDefinition[];
  core: CoreDefinition;
  onGenerateMonthly: () => void;
  onToggleProjectLock: (projectId: string) => void;
}

const formatWorkloadPercent = (workload: number) => {
  return Number((workload * 100).toFixed(2))
    .toString()
    .replace(".", ",");
};

export const TimesheetHeader = ({ projects, core, onGenerateMonthly, onToggleProjectLock }: TimesheetHeaderProps) => {
  return (
    <div className="grid grid-cols-subgrid col-[1/-1] sticky top-0 z-20 self-start bg-slate-100 border-b border-slate-300">
      <div className="sticky left-0 z-40 bg-slate-100 border-r border-slate-300 h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
        Den
      </div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Příchod</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Odchod</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Pauza od</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Pauza do</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Přerušení</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Docházka</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Noční práce</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">STAG</div>
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
        Kmen ({formatWorkloadPercent(core.workload)}%)
      </div>
      {projects.map((project) => (
        <div key={project.id} className="h-10 px-2 flex items-center justify-center gap-1 text-center font-medium whitespace-nowrap min-w-0">
          <Tooltip delayDuration={120}>
            <TooltipTrigger asChild>
              <span className="cursor-help border-b border-dotted border-slate-400">{formatWorkloadPercent(project.workload)}%</span>
            </TooltipTrigger>
            <TooltipContent side="top">{project.registrationNumber || "Bez čísla zakázky"}</TooltipContent>
          </Tooltip>
          <ProjectLockToggleButton locked={project.lockedAt != null} onClick={() => onToggleProjectLock(project.id)} />
        </div>
      ))}
      <div className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">Kontrola</div>
      <div className="sticky right-0 z-40 bg-slate-100 border-r border-slate-300 h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
        <div className="flex items-center justify-center gap-1">
          <span>Rozdíl</span>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 text-blue-500 hover:text-blue-600 hover:bg-blue-50 transition-all active:scale-90"
            onClick={onGenerateMonthly}
            title="Doplnit chybějící hodiny v celém výkazu"
          >
            <Sparkles className="h-3.5 w-3.5 fill-blue-100" />
          </Button>
        </div>
      </div>
    </div>
  );
};
