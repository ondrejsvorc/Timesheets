import { Sparkles } from "lucide-react";
import { useCallback, useMemo } from "react";
import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import type { ProjectDefinition as Project, TimesheetDayEvaluation, TimesheetDay as TimesheetDayModel, TimesheetIssue } from "../../Timesheet";
import { formatHours } from "../../timesheetFormat";
import { Interruption } from "./Interruption";
import { LockableField } from "./LockableField";
import { StagSchedule } from "./StagSchedule";
import { ValidationField } from "./ValidationField";

const cellClass = "min-w-0 p-2 flex items-center justify-center border-border/50";
const numericCellClass = "justify-end text-right tabular-nums";
const cellFirstClass = "sticky left-0 z-10 border-r border-slate-300 bg-white";
const cellLastClass = "sticky right-0 z-10 border-l border-slate-300";
const hoursCellClass = "w-full text-right tabular-nums cursor-help border-b border-dotted border-slate-300";

interface TimesheetDayProps {
  day: TimesheetDayModel;
  dayIndex: number;
  projects: Project[];
  evaluation?: TimesheetDayEvaluation;
  issues: TimesheetIssue[];
  onUpdateDay: (index: number, updater: (day: TimesheetDayModel) => void) => void;
  onAllocate: (day?: number) => Promise<void>;
}

const TimesheetDayComponent = ({ day, dayIndex, projects, evaluation, issues, onUpdateDay, onAllocate }: TimesheetDayProps) => {
  const issuesByField = useMemo(() => {
    const grouped = new Map<string, TimesheetIssue[]>();
    const row: TimesheetIssue[] = [];
    issues.forEach((issue) => {
      if (!issue.field) {
        row.push(issue);
        return;
      }
      grouped.set(issue.field, [...(grouped.get(issue.field) ?? []), issue]);
    });
    return { grouped, row };
  }, [issues]);

  const fieldIssues = useCallback((field: string) => issuesByField.grouped.get(field) ?? [], [issuesByField]);
  const update = (updater: (day: TimesheetDayModel) => void) => onUpdateDay(dayIndex, updater);
  const workedHours = evaluation?.workedHours ?? 0;
  const nightHours = evaluation?.nightHours ?? 0;
  const allocatedHours = evaluation?.allocatedHours ?? 0;
  const balance = evaluation?.balance ?? 0;
  const isWeekendOrHoliday = day.isWeekend || day.isHoliday;
  const shouldLockByInterruption = Boolean(evaluation?.hasCoreOnlyInterruption || evaluation?.hasProportionalInterruption);

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1] border-b border-border/50", isWeekendOrHoliday && "bg-slate-100")}>
      <div className={cn(cellClass, cellFirstClass, isWeekendOrHoliday && "!bg-slate-200")}>
        <ValidationField validations={issuesByField.row}>
          <div className="text-center font-medium">{day.date}</div>
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("clockIn")}>
          <SmartTimeInput
            value={day.attendance.clockIn}
            onChange={(value) =>
              update((draft) => {
                draft.attendance.clockIn = value;
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("clockOut")}>
          <SmartTimeInput
            value={day.attendance.clockOut}
            onChange={(value) =>
              update((draft) => {
                draft.attendance.clockOut = value;
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("breakStart")}>
          <SmartTimeInput
            value={day.attendance.breakStart}
            onChange={(value) =>
              update((draft) => {
                draft.attendance.breakStart = value;
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("breakEnd")}>
          <SmartTimeInput
            value={day.attendance.breakEnd}
            onChange={(value) =>
              update((draft) => {
                draft.attendance.breakEnd = value;
              })
            }
          />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <Interruption value={day.attendance.interruptions} />
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <ValidationField validations={fieldIssues("workedHours")}>
          <HoursToHumanTooltip hours={workedHours}>
            <div className={cn(hoursCellClass, "font-bold")}>{formatHours(workedHours)}</div>
          </HoursToHumanTooltip>
        </ValidationField>
      </div>
      <div className={cn(cellClass, numericCellClass)}>
        <HoursToHumanTooltip hours={nightHours}>
          <div className={cn(hoursCellClass, "text-slate-600")}>{formatHours(nightHours)}</div>
        </HoursToHumanTooltip>
      </div>
      <div className={cellClass}>
        <StagSchedule
          schedules={day.attendance.schedules}
          onSchedulesChange={(schedules) =>
            update((draft) => {
              draft.attendance.schedules = schedules;
            })
          }
          disabled={evaluation?.hasProportionalInterruption}
        />
      </div>
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("coreHours")}>
          <LockableField locked={shouldLockByInterruption}>
            <HoursToHumanTooltip hours={day.coreHours ?? 0}>
              <SmartDecimalInput
                value={day.coreHours}
                onChange={(value) =>
                  update((draft) => {
                    draft.coreHours = value;
                  })
                }
                commitOnChange
                precision={2}
                disabled={shouldLockByInterruption}
                className="h-8 w-20 max-w-full text-right tabular-nums"
              />
            </HoursToHumanTooltip>
          </LockableField>
        </ValidationField>
      </div>
      {projects.map((project) => {
        const locked = project.lockedAt != null || shouldLockByInterruption;
        return (
          <div key={project.id} className={cellClass}>
            <LockableField locked={locked}>
              <HoursToHumanTooltip hours={day.projectHours[project.id] ?? 0}>
                <SmartDecimalInput
                  value={Number(day.projectHours[project.id] ?? 0)}
                  onChange={(value) =>
                    update((draft) => {
                      draft.projectHours[project.id] = value ?? 0;
                    })
                  }
                  commitOnChange
                  precision={2}
                  disabled={locked}
                  className="h-8 w-20 max-w-full text-right tabular-nums"
                />
              </HoursToHumanTooltip>
            </LockableField>
          </div>
        );
      })}
      <div className={cn(cellClass, numericCellClass)}>
        <HoursToHumanTooltip hours={allocatedHours}>
          <div className={cn(hoursCellClass, "text-slate-700 font-semibold")}>{formatHours(allocatedHours)}</div>
        </HoursToHumanTooltip>
      </div>
      <div className={cn(cellClass, numericCellClass, cellLastClass, balance === 0 ? "bg-green-50 text-green-600" : "bg-red-50 text-red-500")}>
        <div className="w-full flex items-center justify-end gap-2">
          <div className="w-full text-right font-bold tabular-nums">{formatHours(balance)}</div>
          <Button
            variant="ghost"
            size="icon"
            className={cn("h-7 w-7 shrink-0 transition-opacity", balance <= 0 ? "opacity-20 cursor-not-allowed" : "opacity-100 text-blue-600 hover:text-blue-700 hover:bg-blue-50")}
            onClick={() => {
              if (balance > 0) void onAllocate(dayIndex + 1);
            }}
            title={Texts.fillRemainingHoursEmptyOnly}
          >
            <Sparkles className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
};

export const TimesheetDay = TimesheetDayComponent;
