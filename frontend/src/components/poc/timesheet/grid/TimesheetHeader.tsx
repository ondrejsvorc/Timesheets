import type { CoreDefinition, ProjectDefinition } from "../../Timesheet";

interface TimesheetHeaderProps {
  projects: ProjectDefinition[];
  core: CoreDefinition;
}

export const TimesheetHeader = ({ projects, core }: TimesheetHeaderProps) => {
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
        Kmen ({core.workload * 100}%)
      </div>
      {projects.map((project) => (
        <div key={project.id} className="h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
          {project.workload * 100}%
        </div>
      ))}
      <div className="sticky right-0 z-40 bg-slate-100 border-r border-slate-300 h-10 px-2 flex items-center justify-center text-center font-medium whitespace-nowrap min-w-0">
        Generovat
      </div>
    </div>
  );
};
