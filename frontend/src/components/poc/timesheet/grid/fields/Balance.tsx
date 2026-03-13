import { cn } from "@/utils/cn";

interface BalanceProps {
  value: number;
}

export const Balance = ({ value }: BalanceProps) => {
  return (
    <div
      className={cn(
        "text-center font-bold tabular-nums",
        value === 0 ? "text-green-600" : "text-red-500"
      )}
    >
      {value === 0 ? "0" : value.toFixed(2)}
    </div>
  );
};