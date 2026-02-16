import { AlertCircle, AlertTriangle, Sparkles } from "lucide-react";
import React, { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/utils/cn";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "../shared/inputs/MultiSelectComboBox";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip";
import { ScheduleCell, ScheduleEditorModal } from "./ScheduleCell";
import { TimeSmartInput } from "./TimeSmartInput";
import type { TimeRange, Timesheet, TimesheetDay } from "./Timesheet";
import { TimesheetLogic } from "./TimesheetLogic";
import { TimesheetValidations } from "./TimesheetValidations";

interface TimesheetTableProps {
  timesheet: Timesheet;
  onUpdateDay: (date: string, recipe: (draftDay: TimesheetDay) => void) => void;
}

export const TimesheetTable = ({ timesheet, onUpdateDay }: TimesheetTableProps) => {
  const [editingDay, setEditingDay] = useState<{ date: string; schedules: TimeRange[] } | null>(null);

  const updateDaySchedules = (date: string, newSchedules: TimeRange[]) => {
    onUpdateDay(date, (draft: TimesheetDay) => {
      draft.attendance.schedules = newSchedules;
    });
  };

  const onUpdateByDate = useCallback(
    (date: string, recipe: (draftDay: TimesheetDay) => void) => {
      onUpdateDay(date, recipe);
    },
    [onUpdateDay],
  );

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
            <TableHead className="min-w-[80px] text-center">Noční práce</TableHead>
            <TableHead className="min-w-[80px] text-center">STAG (hod.)</TableHead>
            <TableHead className="min-w-[80px] text-center">STAG (rozvrh)</TableHead>
            <TableHead className="min-w-[80px] text-center border-l">Kmen ({timesheet.core.workload * 100}%)</TableHead>
            {timesheet.projects.map((project) => (
              <TableHead key={project.id} className="min-w-[80px] text-center px-4">
                <div className="flex flex-col">
                  <span className="whitespace-nowrap">{project.workload * 100}%</span>
                </div>
              </TableHead>
            ))}
            <TableHead className="min-w-[100px] text-center sticky right-0 z-40 bg-muted/90 border-l backdrop-blur-sm shadow-[-5px_0_5px_-5px_rgba(0,0,0,0.1)]">
              <div className="flex items-center justify-center gap-1">
                <span className="text-[10px] font-bold text-slate-500">GENEROVAT</span>
                <TooltipProvider>
                  <Tooltip delayDuration={200}>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-6 w-6 text-blue-500 hover:text-blue-600 hover:bg-blue-50 transition-all active:scale-90"
                        onClick={() => TimesheetLogic.distributeMonthlyHours(timesheet, onUpdateDay)}
                      >
                        <Sparkles className="h-3.5 w-3.5 fill-blue-100" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent side="bottom" className="bg-slate-800 text-white border-none">
                      <p className="text-[11px]">Inteligentně doplnit zbývající hodiny měsíce</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {timesheet.days.map((day) => (
            <TimesheetRow
              key={day.date}
              day={day}
              timesheet={timesheet}
              onUpdate={onUpdateByDate.bind(null, day.date)}
              setEditingDay={setEditingDay}
            />
          ))}
        </TableBody>
        <TimesheetTableFooter timesheet={timesheet} />
      </Table>
      <ScheduleEditorModal
        isOpen={!!editingDay}
        onOpenChange={(open) => !open && setEditingDay(null)}
        initialSchedules={editingDay?.schedules || []}
        dateLabel={editingDay?.date}
        onSave={(newSchedules) => {
          if (editingDay) {
            updateDaySchedules(editingDay.date, newSchedules);
          }
        }}
      />
    </div>
  );
};

interface TimesheetRowProps {
  day: TimesheetDay;
  timesheet: Timesheet;
  onUpdate: (recipe: (draftDay: TimesheetDay) => void) => void;
  setEditingDay: (val: { date: string; schedules: TimeRange[] } | null) => void;
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

const DecimalInput = ({ value, onChange }: { value: number; onChange: (val: number) => void }) => {
  const [inputValue, setInputValue] = useState<string>(value === 0 ? "" : value.toString().replace(".", ","));

  useEffect(() => {
    const normalizedInput = inputValue.replace(",", ".");
    const numericInput = parseFloat(normalizedInput) || 0;
    if (numericInput !== value) {
      const formatted = value === 0 ? "" : value.toString().replace(".", ",");
      setInputValue(formatted);
    }
  }, [value, inputValue]);

  const handleTextChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const nextValue = e.target.value.replace(".", ",");
    if (nextValue === "" || /^\d*,?\d*$/.test(nextValue)) {
      setInputValue(nextValue);
      if (nextValue !== "" && !nextValue.endsWith(",")) {
        const parsed = parseFloat(nextValue.replace(",", "."));
        if (!Number.isNaN(parsed)) {
          onChange(parsed);
        }
      } else if (nextValue === "") {
        onChange(0);
      }
    }
  };

  const handleBlur = () => {
    const parsed = parseFloat(inputValue.replace(",", "."));
    if (Number.isNaN(parsed) || parsed === 0) {
      setInputValue("");
      onChange(0);
    } else {
      const fixed = Number(parsed.toFixed(2));
      setInputValue(fixed.toString().replace(".", ","));
      onChange(fixed);
    }
  };

  return (
    <Input className="w-20 h-8 mx-auto text-right font-medium tabular-nums" value={inputValue} onChange={handleTextChange} onBlur={handleBlur} />
  );
};

export const TimesheetRow = React.memo(
  ({ day, timesheet, onUpdate, setEditingDay }: TimesheetRowProps) => {
    const worked = TimesheetLogic.calculateWorkedHours(day.attendance);
    const workedReadable = TimesheetLogic.formatWorkedHoursToHuman(worked);
    const stagHours = TimesheetLogic.calculateSchedulesTotal(day.attendance.schedules);
    const stagReadable = TimesheetLogic.formatWorkedHoursToHuman(stagHours);
    const isCoreInvalid = !TimesheetLogic.isCoreHoursValid(day);
    const delta = TimesheetLogic.getDelta(day);
    const isWeekend = day.isWeekend;
    const isHoliday = day.isHoliday;

    // Najdeme předchozí den pro validaci odpočinku
    const dayIndex = timesheet.days.findIndex((d) => d.date === day.date);
    const previousDay = dayIndex > 0 ? timesheet.days[dayIndex - 1] : undefined;

    // Validace
    const validations = TimesheetValidations.validateDay(day, previousDay);
    const errors = validations.filter((v) => v.type === "error");
    const warnings = validations.filter((v) => v.type === "warning");
    const hasErrors = errors.length > 0;
    const hasWarnings = warnings.length > 0;

    // Pomocná funkce pro získání validací pro konkrétní pole
    const getFieldValidations = (fieldName: string) => {
      return validations.filter((v) => v.field === fieldName);
    };

    // Komponenta pro input s validací
    const ValidatedTimeInput = ({ field, value, onChange }: { field: string; value: string; onChange: (val: string) => void }) => {
      const fieldValidations = getFieldValidations(field);
      const fieldErrors = fieldValidations.filter((v) => v.type === "error");
      const fieldWarnings = fieldValidations.filter((v) => v.type === "warning");

      return (
        <TooltipProvider delayDuration={100}>
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="relative inline-block">
                <TimeSmartInput value={value} onChange={onChange} />
                {(fieldErrors.length > 0 || fieldWarnings.length > 0) && (
                  <span
                    className={cn(
                      "absolute -top-1 -right-1 size-3 rounded-full border-2 border-white",
                      fieldErrors.length > 0 ? "bg-[#FF9692]" : "bg-[#FFD465]",
                    )}
                  />
                )}
              </div>
            </TooltipTrigger>
            {(fieldErrors.length > 0 || fieldWarnings.length > 0) && (
              <TooltipContent side="top" className="max-w-xs">
                <div className="space-y-1">
                  {fieldErrors.map((err) => (
                    <p key={err.code} className="text-xs font-medium flex items-center gap-1.5" style={{ color: "#FF9692" }}>
                      <AlertCircle className="h-3 w-3 shrink-0" />
                      {err.message}
                    </p>
                  ))}
                  {fieldWarnings.map((warn) => (
                    <p key={warn.code} className="text-xs flex items-center gap-1.5" style={{ color: "#FFD465" }}>
                      <AlertTriangle className="h-3 w-3 shrink-0" />
                      {warn.message}
                    </p>
                  ))}
                </div>
              </TooltipContent>
            )}
          </Tooltip>
        </TooltipProvider>
      );
    };

    console.log("render row", day.date);

    return (
      <TableRow
        className={cn(
          (isWeekend || isHoliday) && "bg-slate-50/50",
          hasErrors && "bg-red-50/30",
          hasWarnings && !hasErrors && "bg-yellow-50/30",
        )}
      >
        <TableCell className={cn("font-medium sticky text-center border-r z-10", isWeekend || isHoliday ? "bg-slate-100/80" : "bg-white")}>
          <div className="flex items-center justify-center gap-1">
            <span className={cn(isWeekend || isHoliday && "text-slate-500")}>
              {day.date} {isHoliday && <span className="text-xs">S</span>}
            </span>
            {(hasErrors || hasWarnings) && (
              <TooltipProvider delayDuration={100}>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span className="cursor-help inline-flex items-center">
                      {hasErrors ? (
                        <AlertCircle className="h-3.5 w-3.5" style={{ color: "#FF9692" }} />
                      ) : (
                        <AlertTriangle className="h-3.5 w-3.5" style={{ color: "#FFD465" }} />
                      )}
                    </span>
                  </TooltipTrigger>
                  <TooltipContent side="right" className="max-w-xs">
                    <div className="space-y-1">
                      {errors.map((err) => (
                        <p key={err.code} className="text-xs font-medium flex items-center gap-1.5" style={{ color: "#FF9692" }}>
                          <AlertCircle className="h-3 w-3 shrink-0" />
                          {err.message}
                        </p>
                      ))}
                      {warnings.map((warn) => (
                        <p key={warn.code} className="text-xs flex items-center gap-1.5" style={{ color: "#FFD465" }}>
                          <AlertTriangle className="h-3 w-3 shrink-0" />
                          {warn.message}
                        </p>
                      ))}
                    </div>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
            )}
          </div>
        </TableCell>

        {/* Attendance Inputs */}
        <TableCell className="text-center">
          <ValidatedTimeInput
            field="clockIn"
            value={day.attendance.clockIn}
            onChange={(val) =>
              onUpdate((d) => {
                d.attendance.clockIn = val;
              })
            }
          />
        </TableCell>
        <TableCell className="text-center">
          <ValidatedTimeInput
            field="clockOut"
            value={day.attendance.clockOut}
            onChange={(val) =>
              onUpdate((d) => {
                d.attendance.clockOut = val;
              })
            }
          />
        </TableCell>
        <TableCell className="text-center">
          <ValidatedTimeInput
            field="breakStart"
            value={day.attendance.breakStart}
            onChange={(val) =>
              onUpdate((d) => {
                d.attendance.breakStart = val;
              })
            }
          />
        </TableCell>
        <TableCell className="text-center">
          <ValidatedTimeInput
            field="breakEnd"
            value={day.attendance.breakEnd}
            onChange={(val) =>
              onUpdate((d) => {
                d.attendance.breakEnd = val;
              })
            }
          />
        </TableCell>

        {/* Interruptions */}
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

        {/* Worked Total */}
        <TableCell className="text-center font-bold tabular-nums">
          <TooltipProvider delayDuration={100}>
            <Tooltip>
              <TooltipTrigger asChild>
                <span className="cursor-help border-b border-dotted border-slate-400">{worked.toFixed(2)}</span>
              </TooltipTrigger>
              <TooltipContent side="top">
                <p className="font-medium text-xs">{workedReadable}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </TableCell>

        {/* Noční práce - readonly, automaticky dopočítané */}
        <TableCell className="text-center">
          <span className="font-bold tabular-nums text-slate-600">{TimesheetLogic.calculateNightHours(day.attendance).toFixed(2)}</span>
        </TableCell>

        {/* STAG (hod) */}
        <TableCell className="text-center font-bold tabular-nums">
          <TooltipProvider delayDuration={100}>
            <Tooltip>
              <TooltipTrigger asChild>
                <span className="cursor-help border-b border-dotted border-slate-300 text-blue-600">
                  {stagHours > 0 ? stagHours.toFixed(2) : "-"}
                </span>
              </TooltipTrigger>
              <TooltipContent side="top">
                <p className="font-medium text-xs">{stagReadable}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </TableCell>

        {/* STAG (rozvrh) */}
        <TableCell className="text-center px-2">
          <ScheduleCell
            schedules={day.attendance.schedules}
            onClick={() =>
              setEditingDay({
                date: day.date,
                schedules: day.attendance.schedules,
              })
            }
          />
        </TableCell>

        {/* Kmen (Core) */}
        <TableCell className={cn("text-center", isCoreInvalid && "bg-red-50")}>
          <div className="flex flex-col items-center gap-0.5">
            <DecimalInput
              value={day.coreHours}
              onChange={(val) =>
                onUpdate((d) => {
                  d.coreHours = val;
                })
              }
            />
            {isCoreInvalid && stagHours > 0 && (
              <span className="text-[9px] text-red-500 font-bold uppercase tracking-tighter">Min: {stagHours.toFixed(1)}</span>
            )}
          </div>
        </TableCell>

        {/* Projects */}
        {timesheet.projects.map((project) => (
          <TableCell key={project.id} className="text-center">
            <DecimalInput
              value={day.projectHours[project.id] || 0}
              onChange={(val) =>
                onUpdate((d) => {
                  d.projectHours[project.id] = val;
                })
              }
            />
          </TableCell>
        ))}

        {/* Delta & Magic Button */}
        <TableCell
          className={cn(
            "text-center font-bold sticky right-0 border-l tabular-nums transition-colors z-30",
            delta === 0 ? "text-green-600 bg-green-50" : "text-red-500 bg-red-50",
          )}
        >
          <div className="flex items-center justify-center gap-2 min-w-[80px]">
            {/* Číslo delty */}
            <span className="flex-1 text-right">{delta === 0 ? "0" : delta.toFixed(2)}</span>

            {/* Tlačítko s bleskem */}
            <Button
              variant="outline"
              size="icon"
              className={cn(
                "h-7 w-7 shrink-0 transition-opacity",
                // Tlačítko svítí jen když chybí hodiny (delta > 0)
                delta <= 0 ? "opacity-20 cursor-not-allowed" : "opacity-100 text-blue-600 border-blue-200 bg-white",
              )}
              onClick={() => {
                if (delta > 0) {
                  const magicFn = TimesheetLogic.distributeRemainingHours(day, timesheet);
                  if (magicFn) magicFn(onUpdate);
                }
              }}
            >
              <Sparkles className="h-4 w-4" />
            </Button>
          </div>
        </TableCell>
      </TableRow>
    );
  },
  (prev, next) => prev.day === next.day,
);

interface TimesheetTableFooterProps {
  timesheet: Timesheet;
}

export const TimesheetTableFooter = ({ timesheet }: TimesheetTableFooterProps) => {
  const { days, projects, core } = timesheet;

  const sum = (fn: (d: TimesheetDay) => number) => days.reduce((acc, d) => acc + fn(d), 0);

  const monthlyTotalWorked = TimesheetLogic.calculateMonthlyTotalWorked(days);
  const monthlyTotalAllocated = TimesheetLogic.calculateMonthlyTotalAllocated(days);
  const monthlyTotalFund = TimesheetLogic.calculateMonthlyFund(timesheet);

  return (
    <TableFooter className="sticky bottom-0 z-50 bg-slate-100 font-bold border-t-2 border-slate-300">
      <TableRow className="bg-slate-200/50 text-[11px] uppercase tracking-wider">
        <TableCell colSpan={6} />

        {/* Odpracováno - celkové hodiny podle docházky */}
        <TableCell className="text-center py-1 whitespace-nowrap tabular-nums">
          {monthlyTotalWorked.toFixed(2)} / {monthlyTotalFund.toFixed(2)}
        </TableCell>

        <TableCell colSpan={3} />

        {/* Kmen: Aktuálně / Fond */}
        {(() => {
          const coreFund = TimesheetLogic.calculateWorkloadFund(timesheet, core.workload);
          const coreCurrent = sum((d) => d.coreHours);
          return (
            <TableCell
              className={cn(
                "text-center py-1 whitespace-nowrap tabular-nums border-l border-slate-300",
                coreCurrent > coreFund + 0.01 ? "text-red-600" : "text-blue-800",
              )}
            >
              {coreCurrent.toFixed(2)} / {coreFund.toFixed(2)}
            </TableCell>
          );
        })()}

        {/* Projekty: Aktuálně / Fond */}
        {projects.map((p) => {
          const projectFund = TimesheetLogic.calculateWorkloadFund(timesheet, p.workload);
          const projectCurrent = sum((d) => d.projectHours[p.id] || 0);
          return (
            <TableCell
              key={p.id}
              className={cn(
                "text-center py-1 whitespace-nowrap tabular-nums",
                projectCurrent > projectFund + 0.01 ? "text-red-600" : "text-blue-800",
              )}
            >
              {projectCurrent.toFixed(2)} / {projectFund.toFixed(2)}
            </TableCell>
          );
        })}

        {/* Kontrolní status vpravo */}
        <TableCell className="sticky right-0 bg-slate-200 border-l border-slate-300">
          <div className="flex justify-center">
            {Math.abs(monthlyTotalAllocated - monthlyTotalFund) < 0.01 ? (
              <span className="text-[9px] text-green-600">OK</span>
            ) : (
              <span className="text-[9px] text-red-500 font-bold">{monthlyTotalAllocated > monthlyTotalFund ? "OVER" : "DIF"}</span>
            )}
          </div>
        </TableCell>
      </TableRow>
    </TableFooter>
  );
};
