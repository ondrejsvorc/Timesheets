import { TimesheetLogic } from "../../../TimesheetLogic";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";

interface NightHoursProps {
  value: number;
}

export const NightHours = ({ value }: NightHoursProps) => {
  return (
    <HoursToHumanTooltip hours={value}>
      <div className="w-full text-right tabular-nums text-slate-600 cursor-help border-b border-dotted border-slate-300">
        {TimesheetLogic.formatHours(value)}
      </div>
    </HoursToHumanTooltip>
  );
};