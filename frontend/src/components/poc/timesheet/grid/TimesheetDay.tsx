import React from "react";
import { cn } from "@/utils/cn";
import type { ProjectDefinition as Project, TimesheetDay as TimesheetDayModel } from "../../Timesheet";
import { TimesheetLogic } from "../../TimesheetLogic";
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
import { useDayValidations } from "./DayValidationsContext";
import { ValidationField } from "./ValidationField";
import { WorkedHours } from "./fields/WorkedHours";

const cellClass = "min-w-0 p-2 flex items-center justify-center border-border/50";
const cellFirstClass = "sticky left-0 z-10 border-r border-slate-300 bg-white";
const cellLastClass = "sticky right-0 z-10 border-l border-slate-300";

interface TimesheetDayProps {
  day: TimesheetDayModel;
  dayIndex: number;
  projects: Project[];
  onUpdateDay: (index: number, updater: (day: TimesheetDayModel) => void) => void;
}

export const TimesheetDay = React.memo(({ day, dayIndex, projects, onUpdateDay }: TimesheetDayProps) => {
  const { workedHours, nightHours, balance } = React.useMemo(() => TimesheetLogic.getDayTotals(day), [day]);
  const validations = useDayValidations();

  const handleUpdateDay = (updater: (day: TimesheetDayModel) => void) => {
    onUpdateDay(dayIndex, updater);
  };

  const isWeekendOrHoliday = day.isWeekend || day.isHoliday;

  return (
    <div className={cn("grid grid-cols-subgrid col-[1/-1] border-b border-border/50", isWeekendOrHoliday && "bg-slate-100")}>
      <div className={cn(cellClass, cellFirstClass, isWeekendOrHoliday && "!bg-slate-200")}>
        <ValidationField validations={validations}>
          <Label label={day.date} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField field="clockIn" validations={validations}>
          <ClockIn value={day.attendance.clockIn} onChange={(v) => handleUpdateDay((d) => { d.attendance.clockIn = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField field="clockOut" validations={validations}>
          <ClockOut value={day.attendance.clockOut} onChange={(v) => handleUpdateDay((d) => { d.attendance.clockOut = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField field="breakStart" validations={validations}>
          <BreakStart value={day.attendance.breakStart} onChange={(v) => handleUpdateDay((d) => { d.attendance.breakStart = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <ValidationField field="breakEnd" validations={validations}>
          <BreakEnd value={day.attendance.breakEnd} onChange={(v) => handleUpdateDay((d) => { d.attendance.breakEnd = v; })} />
        </ValidationField>
      </div>
      <div className={cellClass}>
        <Interruption value={day.attendance.interruptions} onChange={(v) => handleUpdateDay((d) => { d.attendance.interruptions = v; })} />
      </div>
      <div className={cellClass}>
        <WorkedHours value={workedHours} />
      </div>
      <div className={cellClass}>
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
          <ProjectField value={Number(day.projectHours[project.id] ?? 0)} onChange={(v) => handleUpdateDay((d) => { d.projectHours[project.id] = v ?? 0; })} />
        </div>
      ))}
      <div className={cn(cellClass, cellLastClass, balance === 0 ? "bg-green-50 text-green-600" : "bg-red-50 text-red-500")}>
        <Balance value={balance} />
      </div>
    </div>
  );
});
