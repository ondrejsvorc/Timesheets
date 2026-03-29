import { Input } from "@/components/ui/input";
import { cn } from "@/utils/cn";
import type { ComponentProps } from "react";

type WorkloadPercentInputProps = Omit<ComponentProps<typeof Input>, "type">;

/** Celé číslo 0–100; vizuálně s fixní příponou %. */
export const WorkloadPercentInput = ({ className, ...props }: WorkloadPercentInputProps) => {
  return (
    <div className="relative w-full">
      <Input
        {...props}
        type="text"
        inputMode="numeric"
        autoComplete="off"
        className={cn("w-full pr-9 tabular-nums", className)}
      />
      <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground" aria-hidden>
        %
      </span>
    </div>
  );
};
