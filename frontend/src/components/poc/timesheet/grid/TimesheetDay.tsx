import React from "react";
import { Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/utils/cn";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import type { ProjectDefinition as Project, TimesheetDay as TimesheetDayModel } from "../../Timesheet";
import { TimesheetLogic } from "../../TimesheetLogic";
import { TimesheetValidations, type DayValidation } from "../../TimesheetValidations";
import { Balance } from "./fields/Balance";
import { BreakEnd } from "./fields/BreakEnd";
import { BreakStart } from "./fields/BreakStart";
import { ClockIn } from "./fields/ClockIn";
import { ClockOut } from "./fields/ClockOut";
import { CoreEmployment } from "./fields/CoreEmployment";
import { Interruption } from "./fields/Interruption";
import { Label } from "./fields/Label";
import { NightHours } from "./fields/NightHours";
import { Project as ProjectField } from "./fields/Project";
import { StagSchedule } from "./fields/StagSchedule";
import { ValidationField } from "./ValidationField";
import { WorkedHours } from "./fields/WorkedHours";

const cellClass = "min-w-0 p-2 flex items-center justify-center border-border/50";
const numericCellClass = "justify-end text-right tabular-nums";
const cellFirstClass = "sticky left-0 z-10 border-r border-slate-300 bg-white";
const cellLastClass = "sticky right-0 z-10 border-l border-slate-300";

interface TimesheetDayProps {
  day: TimesheetDayModel;
  previousDay?: TimesheetDayModel;
  dayIndex: number;
  projects: Project[];
  totalWorkload: number;
  coreWorkload: number;
  onUpdateDay: (index: number, updater: (day: TimesheetDayModel) => void) => void;
}

const TimesheetDayComponent = ({ day, previousDay, dayIndex, projects, totalWorkload, coreWorkload, onUpdateDay }: TimesheetDayProps) => {
  const { workedHours, nightHours, controlTotal, balance } = React.useMemo(() => TimesheetLogic.getDayTotals(day), [day]);
  const validations = React.useMemo(() => TimesheetValidations.validateDay(day, previousDay), [day, previousDay]);
  const validationsByField = React.useMemo(() => {
    const grouped = new Map<string, DayValidation[]>();
    const rowLevel: DayValidation[] = [];
    validations.forEach((validation) => {
      if (!validation.field) {
        rowLevel.push(validation);
        return;
      }
      const current = grouped.get(validation.field);
      if (current) {
        current.push(validation);
      } else {
        grouped.set(validation.field, [validation]);
      }
    });
    return { grouped, rowLevel };
  }, [validations]);
  const getFieldValidations = React.useCallback(
    (field: string) => validationsByField.grouped.get(field) ?? [],
    [validationsByField]
  );

  const handleUpdateDay = (updater: (day: TimesheetDayModel) => void) => {
    onUpdateDay(dayIndex, updater);
  };

  const isWeekendOrHoliday = day.isWeekend || day.isHoliday;

  return (
    <div
      className={cn(
        "grid grid-cols-subgrid col-[1/-1] border-b border-border/50",
        isWeekendOrHoliday && "bg-slate-100"
      )}
    >
      <div className={cn(cellClass, cellFirstClass, isWeekendOrHoliday && "!bg-slate-200")}>
        <ValidationField validations={validationsByField.rowLevel}>
          <Label label={day.date} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("clockIn")}>
          <ClockIn value={day.attendance.clockIn} onChange={(v) => handleUpdateDay((d) => { d.attendance.clockIn = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("clockOut")}>
          <ClockOut value={day.attendance.clockOut} onChange={(v) => handleUpdateDay((d) => { d.attendance.clockOut = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("breakStart")}>
          <BreakStart value={day.attendance.breakStart} onChange={(v) => handleUpdateDay((d) => { d.attendance.breakStart = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("breakEnd")}>
          <BreakEnd value={day.attendance.breakEnd} onChange={(v) => handleUpdateDay((d) => { d.attendance.breakEnd = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <Interruption value={day.attendance.interruptions} onChange={(v) => handleUpdateDay((d) => { d.attendance.interruptions = v; })} />
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <WorkedHours value={workedHours} />
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <NightHours value={nightHours} />
      </div>
      <div className={cellClass}>
        <StagSchedule schedules={day.attendance.schedules} onSchedulesChange={(newSchedules) => handleUpdateDay((d) => { d.attendance.schedules = newSchedules; })} />
      </div>
      <div className={cellClass}>
        <CoreEmployment value={day.coreHours} onChange={(v) => handleUpdateDay((d) => { d.coreHours = v; })} />
      </div>
      {projects.map((project) => (
        <div key={project.id} className={cellClass}>
          <ProjectField
            value={Number(day.projectHours[project.id] ?? 0)}
            locked={project.lockedAt != null}
            onChange={(v) => handleUpdateDay((d) => { d.projectHours[project.id] = v ?? 0; })}
          />
        </div>
      ))}
      <div className={cn(cellClass, numericCellClass)}>
        <HoursToHumanTooltip hours={controlTotal}>
          <div className="cursor-help border-b border-dotted border-slate-300 text-slate-700 font-semibold">
            {TimesheetLogic.formatHours(controlTotal)}
          </div>
        </HoursToHumanTooltip>
      </div>
      <div className={cn(cellClass, numericCellClass, cellLastClass, balance === 0 ? "bg-green-50 text-green-600" : "bg-red-50 text-red-500")}>
        <div className="w-full flex items-center justify-end gap-2">
          <Balance value={balance} />
          <Button
            variant="ghost"
            size="icon"
            className={cn(
              "h-7 w-7 shrink-0 transition-opacity",
              balance <= 0 ? "opacity-20 cursor-not-allowed" : "opacity-100 text-blue-600 hover:text-blue-700 hover:bg-blue-50"
            )}
            onClick={() => {
              if (balance > 0) {
                const magicFn = TimesheetLogic.distributeRemainingHours(day, totalWorkload, coreWorkload, projects);
                if (magicFn) {
                  magicFn(handleUpdateDay);
                }
              }
            }}
            title="Doplnit zbývající hodiny jen do prázdných polí"
          >
            <Sparkles className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
};

export const TimesheetDay = React.memo(
  TimesheetDayComponent,
  (prev, next) =>
    prev.day === next.day &&
    prev.previousDay === next.previousDay &&
    prev.dayIndex === next.dayIndex &&
    prev.projects === next.projects &&
    prev.totalWorkload === next.totalWorkload &&
    prev.coreWorkload === next.coreWorkload &&
    prev.onUpdateDay === next.onUpdateDay
);
