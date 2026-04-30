import { Sparkles } from "lucide-react";
import React from "react";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { Button } from "@/components/ui/button";
import { cn } from "@/utils/cn";
import type { ProjectDefinition as Project, TimesheetDay as TimesheetDayModel } from "../../Timesheet";
import { TimesheetLogic } from "../../TimesheetLogic";
import { type DayValidation, TimesheetValidations } from "../../TimesheetValidations";
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
import { WorkedHours } from "./fields/WorkedHours";
import { LockableField } from "./LockableField";
import { ValidationField } from "./ValidationField";

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
  const getFieldValidations = React.useCallback((field: string) => validationsByField.grouped.get(field) ?? [], [validationsByField]);

  const handleUpdateDay = (updater: (day: TimesheetDayModel) => void) => {
    onUpdateDay(dayIndex, updater);
  };

  const isWeekendOrHoliday = day.isWeekend || day.isHoliday;
  const hasInterruption = Boolean(day.attendance.interruptions?.trim());
  const hasBusinessTripInterruption = TimesheetLogic.hasBusinessTripInterruption(day.attendance);
  const hasProportionalInterruption = hasInterruption && !hasBusinessTripInterruption && !TimesheetLogic.hasCoreOnlyInterruption(day.attendance);
  const shouldAutoAllocateInterruption = hasInterruption && !hasBusinessTripInterruption;
  const shouldLockByInterruption = shouldAutoAllocateInterruption;

  const applyInterruptionAutofill = (draft: TimesheetDayModel) => {
    if (!draft.attendance.interruptions?.trim()) {
      return;
    }

    if (TimesheetLogic.hasBusinessTripInterruption(draft.attendance)) {
      return;
    }

    const isProportionalInterruption = !TimesheetLogic.hasCoreOnlyInterruption(draft.attendance);
    if (isProportionalInterruption) {
      // For proportional interruptions (e.g. vacation), attendance/stag inputs are not applicable.
      draft.attendance.clockIn = "";
      draft.attendance.clockOut = "";
      draft.attendance.breakStart = "";
      draft.attendance.breakEnd = "";
      draft.attendance.schedules = [];
    }

    const interruptionHours = TimesheetLogic.calculateInterruptionCoreHours(draft, totalWorkload);
    const projectsTotalWorkload = projects.reduce((sum, p) => sum + p.workload, 0);
    const workloadSum = Math.max(0, coreWorkload + projectsTotalWorkload);

    if (workloadSum <= 0) {
      draft.coreHours = 0;
      Object.keys(draft.projectHours).forEach((projectId) => {
        draft.projectHours[projectId] = 0;
      });
      return;
    }

    if (TimesheetLogic.hasCoreOnlyInterruption(draft.attendance)) {
      draft.coreHours = interruptionHours;
      Object.keys(draft.projectHours).forEach((projectId) => {
        draft.projectHours[projectId] = 0;
      });
      return;
    }

    const toCents = (value: number): number => Math.max(0, Math.round(value * 100));
    const fromCents = (value: number): number => Number((Math.max(0, value) / 100).toFixed(2));
    const totalCents = toCents(interruptionHours);
    const projectIds = projects.map((p) => p.id);
    const nextProjectCents: Record<string, number> = {};

    let allocatedProjectCents = 0;
    projects.forEach((project) => {
      const cents = Math.round((totalCents * project.workload) / workloadSum);
      nextProjectCents[project.id] = Math.max(0, cents);
      allocatedProjectCents += Math.max(0, cents);
    });

    const coreCents = Math.max(0, totalCents - allocatedProjectCents);
    draft.coreHours = fromCents(coreCents);
    projectIds.forEach((projectId) => {
      draft.projectHours[projectId] = fromCents(nextProjectCents[projectId] ?? 0);
    });
  };

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1] border-b border-border/50", isWeekendOrHoliday && "bg-slate-100")}>
      <div className={cn(cellClass, cellFirstClass, isWeekendOrHoliday && "!bg-slate-200")}>
        <ValidationField validations={validationsByField.rowLevel}>
          <Label label={day.date} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("clockIn")}>
          <ClockIn
            value={day.attendance.clockIn}
            onChange={(v) =>
              handleUpdateDay((d) => {
                d.attendance.clockIn = v;
                applyInterruptionAutofill(d);
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("clockOut")}>
          <ClockOut
            value={day.attendance.clockOut}
            onChange={(v) =>
              handleUpdateDay((d) => {
                d.attendance.clockOut = v;
                applyInterruptionAutofill(d);
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("breakStart")}>
          <BreakStart
            value={day.attendance.breakStart}
            onChange={(v) =>
              handleUpdateDay((d) => {
                d.attendance.breakStart = v;
                applyInterruptionAutofill(d);
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={getFieldValidations("breakEnd")}>
          <BreakEnd
            value={day.attendance.breakEnd}
            onChange={(v) =>
              handleUpdateDay((d) => {
                d.attendance.breakEnd = v;
                applyInterruptionAutofill(d);
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <Interruption value={day.attendance.interruptions} />
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <WorkedHours value={workedHours} />
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <NightHours value={nightHours} />
      </div>
      <div className={cellClass}>
        <StagSchedule
          schedules={day.attendance.schedules}
          onSchedulesChange={(newSchedules) =>
            handleUpdateDay((d) => {
              d.attendance.schedules = newSchedules;
              applyInterruptionAutofill(d);
            })
          }
          disabled={hasProportionalInterruption}
        />
      </div>
      <div className={cellClass}>
        <LockableField locked={shouldLockByInterruption}>
          <CoreEmployment
            value={day.coreHours}
            disabled={shouldLockByInterruption}
            onChange={(v) =>
              handleUpdateDay((d) => {
                d.coreHours = v;
              })
            }
          />
        </LockableField>
      </div>
      {projects.map((project) => (
        <div key={project.id} className={cellClass}>
          <LockableField locked={project.lockedAt != null || shouldLockByInterruption}>
            <ProjectField
              value={Number(day.projectHours[project.id] ?? 0)}
              locked={project.lockedAt != null || shouldLockByInterruption}
              onChange={(v) =>
                handleUpdateDay((d) => {
                  d.projectHours[project.id] = v ?? 0;
                })
              }
            />
          </LockableField>
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
              balance <= 0 ? "opacity-20 cursor-not-allowed" : "opacity-100 text-blue-600 hover:text-blue-700 hover:bg-blue-50",
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
    prev.onUpdateDay === next.onUpdateDay,
);
