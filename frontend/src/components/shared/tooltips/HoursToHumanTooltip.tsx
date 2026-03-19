import type { ReactNode } from "react";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

interface HoursToHumanTooltipProps {
  hours: number;
  children: ReactNode;
}

const formatHoursToHuman = (hours: number): string => {
  const sign = hours < 0 ? "-" : "";
  const totalMinutes = Math.round(Math.abs(hours) * 60);
  const wholeHours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return `${sign}${wholeHours}h ${minutes}m`;
};

export const HoursToHumanTooltip = ({ hours, children }: HoursToHumanTooltipProps) => {
  return (
    <Tooltip delayDuration={100}>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent side="top">
        <p className="font-medium text-xs">{formatHoursToHuman(hours)}</p>
      </TooltipContent>
    </Tooltip>
  );
};
