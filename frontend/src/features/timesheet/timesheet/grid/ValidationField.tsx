import { AlertCircle, AlertTriangle } from "lucide-react";
import type { ReactNode } from "react";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/utils/common";
import type { TimesheetIssue } from "../../Timesheet";

interface ValidationFieldProps {
  validations: TimesheetIssue[];
  children: ReactNode;
}

export const ValidationField = ({ validations, children }: ValidationFieldProps) => {
  const errors = validations.filter((v) => v.type === "error");
  const warnings = validations.filter((v) => v.type === "warning");
  const hasAny = errors.length > 0 || warnings.length > 0;
  const tooltipText = hasAny ? [...errors, ...warnings].map((v) => v.message).join("\n") : undefined;
  const dotClass = cn("size-2.5 rounded-full ring-2 ring-white pointer-events-none", errors.length > 0 ? "bg-[#FF9692]" : "bg-[#FFD465]");

  return (
    <div className="flex justify-center w-full min-w-0">
      <Tooltip>
        <TooltipTrigger asChild>
          <div className="relative inline-block" title={tooltipText} tabIndex={hasAny ? 0 : undefined} aria-invalid={errors.length > 0 || undefined}>
            {children}
            {hasAny && <span className={cn("absolute right-0 top-0 z-10 translate-x-1/2 -translate-y-1/2", dotClass)} />}
          </div>
        </TooltipTrigger>
        {hasAny && (
          <TooltipContent side="top" className="max-w-xs z-[100]">
            <div className="space-y-1">
              {errors.map((err) => (
                <p key={err.code} className="text-xs font-medium flex items-center gap-1.5" style={{ color: "#FF9692" }}>
                  <AlertCircle className="h-3 w-3 shrink-0" />
                  {err.message}
                </p>
              ))}
              {warnings.map((warn) => (
                <p key={warn.code} className="text-xs flex items-center gap-1.5" style={{ color: "#FFD465" }}>
                  <AlertTriangle className="h-3 w-3 shrink-0" />
                  {warn.message}
                </p>
              ))}
            </div>
          </TooltipContent>
        )}
      </Tooltip>
    </div>
  );
};
