import { Lock, Sparkles, Unlock } from "lucide-react";
import { useCallback, useMemo } from "react";
import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/common";
import { formatHours } from "@/utils/format";
import type { ProjectDefinition as Project, TimesheetDayEvaluation, TimesheetDay as TimesheetDayModel, TimesheetIssue } from "../Timesheet";
import { Interruption } from "./Interruption";
import { LockableField } from "./LockableField";
import { StagSchedule } from "./StagSchedule";
import { ValidationField } from "./ValidationField";

const cellClass = "min-w-0 p-2 flex items-center justify-center border-border/50";
const numericCellClass = "justify-end text-right tabular-nums";
const cellFirstClass = "sticky left-0 z-10 border-r border-slate-300 bg-white";
const cellLastClass = "sticky right-0 z-10 border-l border-slate-300";
const hoursCellClass = "w-full text-right tabular-nums cursor-help border-b border-dotted border-slate-300";
const dayLevelFields = new Set(["workedHours", "allocatedHours"]);

const roundHours = (hours: number) => Math.round(Math.max(0, hours) * 100) / 100;

const timeToMinutes = (time: string) => {
  const [hours, minutes] = time.slice(0, 5).split(":").map(Number);
  return Number.isFinite(hours) && Number.isFinite(minutes) ? hours * 60 + minutes : 0;
};

const calculateStagHours = (day: TimesheetDayModel) =>
  roundHours(
    Math.min(
      12,
      day.attendance.schedules.reduce((total, schedule) => {
        const start = timeToMinutes(schedule.start);
        const end = timeToMinutes(schedule.end);
        return end > start ? total + (end - start) / 60 : total;
      }, 0),
    ),
  );

interface TimesheetDayProps {
  tracksAttendance: boolean;
  day: TimesheetDayModel;
  dayIndex: number;
  projects: Project[];
  evaluation?: TimesheetDayEvaluation;
  issues: TimesheetIssue[];
  onUpdateDay: (index: number, updater: (day: TimesheetDayModel) => void) => void;
  onAllocate: (day?: number) => Promise<void>;
}

const TimesheetDayComponent = ({ tracksAttendance, day, dayIndex, projects, evaluation, issues, onUpdateDay, onAllocate }: TimesheetDayProps) => {
  const issuesByField = useMemo(() => {
    const grouped = new Map<string, TimesheetIssue[]>();
    const row: TimesheetIssue[] = [];
    issues.forEach((issue) => {
      if (!issue.field || dayLevelFields.has(issue.field)) {
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
  const coreLocked = shouldLockByInterruption;
  const allocatedInputHours = (day.coreHours ?? 0) + Object.values(day.projectCells).reduce((sum, cell) => sum + cell.hours, 0);
  const canGenerateAttendance = tracksAttendance && issues.some((issue) => issue.code === "ERR-ATT-13") && (day.attendance.schedules.length > 0 || allocatedInputHours > 0);
  const stagMissing = !tracksAttendance && issues.some((issue) => issue.code === "ERR-ALL-02") ? roundHours(calculateStagHours(day) - (day.coreHours ?? 0)) : 0;
  const displayBalance = stagMissing > 0 ? Math.max(balance, stagMissing) : balance;
  const canAllocateRow = displayBalance > 0 || canGenerateAttendance;

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1] border-b border-border/50", isWeekendOrHoliday && "bg-slate-100")}>
      <div className={cn(cellClass, cellFirstClass, isWeekendOrHoliday && "!bg-slate-200")}>
        <ValidationField validations={issuesByField.row}>
          <div className="text-center font-medium">{day.date}</div>
        </ValidationField>
      </div>
      {tracksAttendance && (
        <>
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
        </>
      )}
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("interruptions")}>
          <Interruption value={day.attendance.interruptions} />
        </ValidationField>
      </div>
      {tracksAttendance && (
        <>
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
        </>
      )}
      {!tracksAttendance && (
        <div className={cellClass}>
          <ValidationField validations={fieldIssues("schedules")}>
            <StagSchedule
              schedules={day.attendance.schedules}
              onSchedulesChange={(schedules) =>
                update((draft) => {
                  draft.attendance.schedules = schedules;
                })
              }
              disabled={evaluation?.hasProportionalInterruption}
            />
          </ValidationField>
        </div>
      )}
      <div className={cellClass}>
        <ValidationField validations={fieldIssues("coreHours")}>
          <LockableField locked={coreLocked}>
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
                disabled={coreLocked}
                className="h-8 w-20 max-w-full text-right tabular-nums"
              />
            </HoursToHumanTooltip>
          </LockableField>
        </ValidationField>
      </div>
      {projects.map((project) => {
        const active = project.activeDays[dayIndex] ?? true;
        const cell = day.projectCells[project.id] ?? { hours: 0, locked: false };
        const systemLocked = project.locked || shouldLockByInterruption || !active;
        const locked = systemLocked || cell.locked;
        const lockLabel = cell.locked ? Texts.unlockProjectCell : Texts.lockProjectCell;
        return (
          <div key={project.id} className={cellClass}>
            <ValidationField validations={fieldIssues(`project:${project.id}`)}>
              <div className="flex w-full items-center justify-end gap-1">
                <LockableField locked={systemLocked}>
                  <HoursToHumanTooltip hours={cell.hours}>
                    <SmartDecimalInput
                      value={Number(cell.hours)}
                      onChange={(value) =>
                        update((draft) => {
                          const current = draft.projectCells[project.id] ?? { hours: 0, locked: false };
                          draft.projectCells[project.id] = { ...current, hours: value ?? 0 };
                        })
                      }
                      commitOnChange
                      precision={2}
                      disabled={locked}
                      className="h-8 w-20 max-w-full text-right tabular-nums"
                    />
                  </HoursToHumanTooltip>
                </LockableField>
                <Button
                  variant="ghost"
                  size="icon"
                  disabled={systemLocked}
                  className={cn("h-7 w-7 shrink-0", cell.locked ? "text-primary" : "text-slate-500")}
                  onClick={() =>
                    update((draft) => {
                      const current = draft.projectCells[project.id] ?? { hours: 0, locked: false };
                      draft.projectCells[project.id] = { ...current, locked: !current.locked };
                    })
                  }
                  title={lockLabel}
                  aria-label={lockLabel}
                >
                  {cell.locked || systemLocked ? <Lock className="h-4 w-4" /> : <Unlock className="h-4 w-4" />}
                </Button>
              </div>
            </ValidationField>
          </div>
        );
      })}
      <div className={cn(cellClass, numericCellClass)}>
        <ValidationField validations={fieldIssues("allocatedHours")}>
          <HoursToHumanTooltip hours={allocatedHours}>
            <div className={cn(hoursCellClass, "text-slate-700 font-semibold")}>{formatHours(allocatedHours)}</div>
          </HoursToHumanTooltip>
        </ValidationField>
      </div>
      <div className={cn(cellClass, numericCellClass, cellLastClass, displayBalance === 0 ? "bg-green-50 text-green-600" : "bg-red-50 text-red-500")}>
        <ValidationField validations={fieldIssues("balance")}>
          <div className="w-full flex items-center justify-end gap-2">
            <div className="w-full text-right font-bold tabular-nums">{formatHours(displayBalance)}</div>
            <Button
              variant="ghost"
              size="icon"
              disabled={!canAllocateRow}
              className={cn("h-7 w-7 shrink-0 transition-opacity", !canAllocateRow ? "opacity-20 cursor-not-allowed" : "opacity-100 text-blue-600 hover:text-blue-700 hover:bg-blue-50")}
              onClick={() => {
                if (canAllocateRow) void onAllocate(dayIndex + 1);
              }}
              title={Texts.fillRemainingHoursEmptyOnly}
            >
              <Sparkles className="h-4 w-4" />
            </Button>
          </div>
        </ValidationField>
      </div>
    </div>
  );
};

export const TimesheetDay = TimesheetDayComponent;
