import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TimeSmartInput } from "./TimeSmartInput";
import type { ProjectDefinition, Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetLogic } from "./TimesheetLogic";

interface TimesheetTableProps {
  timesheet: Timesheet;
  onUpdateDay: (date: string, recipe: (draftDay: TimesheetDay) => void) => void;
}

export const TimesheetTable = ({ timesheet, onUpdateDay }: TimesheetTableProps) => {
  return (
    <div className="rounded-md border-t border-l border-slate-300 overflow-auto max-h-[calc(100vh-100px)] w-full relative border-separate border-spacing-0 shadow-sm">
      <Table className="min-w-max w-full border-separate border-spacing-0">
        <TableHeader className="sticky top-0 z-20 bg-muted">
          <TableRow>
            <TableHead className="w-32 min-w-[120px] sticky left-0 z-40 bg-muted border-r text-center">Den</TableHead>
            <TableHead className="min-w-[80px] text-center">Příchod</TableHead>
            <TableHead className="min-w-[80px] text-center">Odchod</TableHead>
            <TableHead className="min-w-[80px] text-center">Pauza od</TableHead>
            <TableHead className="min-w-[80px] text-center">Pauza do</TableHead>
            <TableHead className="min-w-[80px] text-center">Přerušení</TableHead>
            <TableHead className="min-w-[80px] text-center">Odpracováno</TableHead>
            <TableHead className="min-w-[80px] text-center">STAG (hod)</TableHead>
            <TableHead className="min-w-[80px] text-center">STAG (rozvrh)</TableHead>
            <TableHead className="min-w-[80px] text-center border-l">Kmen ({timesheet.core.workload * 100}%)</TableHead>
            {timesheet.projects.map((project) => (
              <TableHead key={project.id} className="min-w-[80px] text-center px-4">
                <div className="flex flex-col">
                  <span className="whitespace-nowrap">{project.workload * 100}%</span>
                </div>
              </TableHead>
            ))}
            <TableHead className="min-w-[80px] text-center sticky right-0 z-40 bg-muted/90 border-l backdrop-blur-sm">Kontrola</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {timesheet.days.map((day) => (
            <TimesheetRow
              key={day.date}
              day={day}
              projects={timesheet.projects}
              coreWorkload={timesheet.core.workload}
              onUpdate={(recipe) => onUpdateDay(day.date, recipe)}
            />
          ))}
        </TableBody>
        <TimesheetTableFooter days={timesheet.days} projects={timesheet.projects} />
      </Table>
    </div>
  );
};

interface TimesheetRowProps {
  day: TimesheetDay;
  projects: ProjectDefinition[];
  coreWorkload: number;
  onUpdate: (recipe: (draftDay: TimesheetDay) => void) => void;
}

const TimesheetRow = ({ day, projects, onUpdate }: TimesheetRowProps) => {
  const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
  const delta = TimesheetLogic.getDelta(day);

  return (
    <TableRow>
      <TableCell className="font-medium sticky text-center bg-white border-r">{day.date}</TableCell>
      {/* Příchod */}
      <TableCell className="text-center">
        <TimeSmartInput
          value={day.attendance.clockIn}
          onChange={(val) =>
            onUpdate((d) => {
              d.attendance.clockIn = val;
            })
          }
        />
      </TableCell>

      {/* Odchod */}
      <TableCell className="text-center">
        <TimeSmartInput
          value={day.attendance.clockOut}
          onChange={(val) =>
            onUpdate((d) => {
              d.attendance.clockOut = val;
            })
          }
        />
      </TableCell>

      {/* Pauza Start */}
      <TableCell className="text-center">
        <TimeSmartInput
          value={day.attendance.breakStart}
          onChange={(val) =>
            onUpdate((d) => {
              d.attendance.breakStart = val;
            })
          }
        />
      </TableCell>

      {/* Pauza Konec */}
      <TableCell className="text-center">
        <TimeSmartInput
          value={day.attendance.breakEnd}
          onChange={(val) =>
            onUpdate((d) => {
              d.attendance.breakEnd = val;
            })
          }
        />
      </TableCell>
      <TableCell className="text-center">
        <Input
          type="text"
          className="w-20 h-8 mx-auto"
          value={day.attendance.interruptions || ""}
          onChange={(e) =>
            onUpdate((draftDay) => {
              draftDay.attendance.interruptions = e.target.value;
            })
          }
        />
      </TableCell>
      <TableCell className="text-center font-bold tabular-nums">{worked.toFixed(2)}</TableCell>
      <TableCell className="text-center"></TableCell>
      <TableCell className="text-center"></TableCell>
      <TableCell className="text-center">
        <Input
          type="number"
          step="0.25"
          className="w-20 h-8 mx-auto text-right"
          value={day.coreHours || ""}
          onChange={(e) =>
            onUpdate((draftDay) => {
              draftDay.coreHours = parseFloat(e.target.value) || 0;
            })
          }
        />
      </TableCell>
      {projects.map((project) => (
        <TableCell key={project.id} className="text-center">
          <Input
            type="number"
            step="0.25"
            className="w-20 h-8 mx-auto text-right"
            value={day.projectHours[project.id] || ""}
            onChange={(e) =>
              onUpdate((draftDay) => {
                draftDay.projectHours[project.id] = parseFloat(e.target.value) || 0;
              })
            }
          />
        </TableCell>
      ))}
      <TableCell
        className={`text-center font-bold sticky right-0 border-l tabular-nums ${delta === 0 ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50"}`}
      >
        {delta === 0 ? "0" : delta.toFixed(2)}
      </TableCell>
    </TableRow>
  );
};

interface TimesheetTableFooterProps {
  days: TimesheetDay[];
  projects: ProjectDefinition[];
}

export const TimesheetTableFooter = ({ days, projects }: TimesheetTableFooterProps) => {
  const sum = (fn: (d: TimesheetDay) => number) => days.reduce((acc, d) => acc + fn(d), 0).toFixed(2);

  // Date Column (1) + Time Columns (5) + Worked Column (1) + STAG columns (2) = 9
  const leadColumnsCount = 9;

  return (
    <TableFooter className="sticky bottom-0 z-50 bg-slate-100 font-bold">
      <TableRow>
        <TableCell colSpan={leadColumnsCount} />
        <TableCell className="text-center">{sum((d) => d.coreHours)}</TableCell>
        {projects.map((p) => (
          <TableCell key={p.id} className="text-center">
            {sum((d) => d.projectHours[p.id] || 0)}
          </TableCell>
        ))}
        <TableCell className="text-center sticky right-0 bg-slate-100 border-l"></TableCell>
      </TableRow>
    </TableFooter>
  );
};
