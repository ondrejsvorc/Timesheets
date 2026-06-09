import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import { TimesheetLogic } from "../../../TimesheetLogic";

interface WorkedHoursProps {
  value: number;
}

export const WorkedHours = ({ value }: WorkedHoursProps) => {
  return (
    <HoursToHumanTooltip hours={value}>
      <div className="w-full text-right font-bold tabular-nums cursor-help border-b border-dotted border-slate-300">
        {TimesheetLogic.formatHours(value)}
      </div>
    </HoursToHumanTooltip>
  );
};
