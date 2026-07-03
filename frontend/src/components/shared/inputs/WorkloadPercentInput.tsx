import type { ComponentProps } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/utils/common";

type WorkloadPercentInputProps = Omit<ComponentProps<typeof Input>, "type">;

/** Číslo 0–100; vizuálně s fixní příponou %. */
export const WorkloadPercentInput = ({ className, ...props }: WorkloadPercentInputProps) => {
  return (
    <div className="relative w-full">
      <Input {...props} type="text" inputMode="decimal" autoComplete="off" className={cn("w-full pr-9 tabular-nums", className)} />
      <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground" aria-hidden>
        %
      </span>
    </div>
  );
};
