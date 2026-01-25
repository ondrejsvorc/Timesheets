import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "../shared/inputs/MultiSelectComboBox";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip";
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

export const INTERRUPTION_OPTIONS: MultiSelectComboBoxItem[] = [
  { value: "D", label: "D – Dovolenka" },
  { value: "JMV/HO", label: "JMV/HO – práce na dálku od 1.10.2023" },
  { value: "KAHO", label: "KAHO – Karanténa -home office" },
  { value: "M", label: "M – Omluvená nepřítomnost - tvůrčí volno" },
  { value: "MD/OD", label: "MD/OD – Mateřská / Otcovská dovolená" },
  { value: "N", label: "N – Nemocenská" },
  { value: "NA", label: "NA – Neomluvená absence" },
  { value: "NK", label: "NK – Návštěva lékaře - krátkodobá" },
  { value: "NL", label: "NL – Návštěva lékaře - celý den" },
  { value: "NP", label: "NP – Pracovní úraz" },
  { value: "NV", label: "NV – Náhradní volno" },
  { value: "O", label: "O – Ošetřovné" },
  { value: "OPN", label: "OPN – Osobní překážky" },
  { value: "PN", label: "PN – Narození dítěte" },
  { value: "PO", label: "PO – Odběr krve" },
  { value: "PS", label: "PS – Svatba" },
  { value: "PU", label: "PU – Úmrtí rod. příslušníka" },
  { value: "PVB", label: "PVB – Pracovní volno - branná povinnost" },
  { value: "PVM", label: "PVM – Pracovní volno - akce pro děti" },
  { value: "PZ", label: "PZ – Překážka na straně zaměstnavatele" },
  { value: "RD", label: "RD – Rodičovská dovolená" },
  { value: "SCP", label: "SCP – Tuzemská služební cesta Projekt" },
  { value: "SCS", label: "SCS – Tuzemská služební cesta Stáž" },
  { value: "SCT", label: "SCT – Služební cesta" },
  { value: "SCZ", label: "SCZ – Služební cesta zahraniční" },
  { value: "SCZE", label: "SCZE – Zahraniční cesta Erasmus" },
  { value: "SCZP", label: "SCZP – Zahraniční cesta Projekt" },
  { value: "SCZS", label: "SCZS – Zahraniční cesta Stáž" },
  { value: "ST", label: "ST – Studium s náhradou mzdy" },
  { value: "VN", label: "VN – Neplacené volno" },
  { value: "VZ", label: "VZ – Nové zaměstnání" },
  { value: "Z", label: "Z – Volno pro obecný zájem" },
  { value: "Zp", label: "Zp – Veřejná funkce - poslanec" },
  { value: "Zs", label: "Zs – Dlouhodobý pobyt v cizině" },
  { value: "Zv", label: "Zv – Zdravotní volno" },
];

const TimesheetRow = ({ day, projects, onUpdate }: TimesheetRowProps) => {
  const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
  const workedHumanReadable = TimesheetLogic.formatWorkedHoursToHuman(worked);
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
        <MultiSelectComboBox
          items={INTERRUPTION_OPTIONS}
          placeholder="Vyberte..."
          value={day.attendance.interruptions ? day.attendance.interruptions.split(",").filter(Boolean) : []}
          onChange={(selectedArray) =>
            onUpdate((draftDay) => {
              draftDay.attendance.interruptions = selectedArray.join(",");
            })
          }
        />
      </TableCell>
      <TableCell className="text-center font-bold tabular-nums">
        <TooltipProvider delayDuration={100}>
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="cursor-help border-b border-dotted border-slate-400">{worked.toFixed(2)}</span>
            </TooltipTrigger>
            <TooltipContent side="top">
              <p className="font-medium text-xs">{workedHumanReadable}</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </TableCell>
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
