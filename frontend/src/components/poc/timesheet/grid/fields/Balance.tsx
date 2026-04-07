import { cn } from "@/utils/cn";
import { TimesheetLogic } from "../../../TimesheetLogic";

interface BalanceProps {
  value: number;
}

export const Balance = ({ value }: BalanceProps) => {
  return (
    <div className={cn("w-full text-right font-bold tabular-nums", value === 0 ? "text-green-600" : "text-red-500")}>
      {value === 0 ? "0" : TimesheetLogic.formatHours(value)}
    </div>
  );
};
