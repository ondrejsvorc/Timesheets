import { AlertCircle, AlertTriangle } from "lucide-react";
import type { ReactNode } from "react";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { cn } from "@/utils/cn";
import type { DayValidation } from "../../TimesheetValidations";

interface ValidationFieldProps {
  field?: string;
  validations: DayValidation[];
  children: ReactNode;
}

export const ValidationField = ({ field, validations, children }: ValidationFieldProps) => {
  const fieldValidations = field === undefined ? validations.filter((v) => v.field == null) : validations.filter((v) => v.field === field);
  const errors = fieldValidations.filter((v) => v.type === "error");
  const warnings = fieldValidations.filter((v) => v.type === "warning");
  const hasAny = errors.length > 0 || warnings.length > 0;

  const tooltipText = hasAny
    ? [...errors, ...warnings].map((v) => v.message).join("\n")
    : undefined;

  const isRowLevel = field === undefined;
  const dotClass = cn(
    "size-3 rounded-full border-2 border-white shrink-0 pointer-events-none",
    errors.length > 0 ? "bg-[#FF9692]" : "bg-[#FFD465]",
  );

  return (
    <div className="flex justify-center w-full min-w-0">
      <TooltipProvider delayDuration={0}>
        <Tooltip>
          <TooltipTrigger asChild>
            <div
              className={cn(
                isRowLevel ? "inline-flex items-center gap-1.5" : "relative inline-block",
              )}
              title={tooltipText}
            >
              {children}
              {hasAny && (
                isRowLevel ? (
                  <span className={dotClass} />
                ) : (
                  <span className={cn("absolute -top-1 -right-1", dotClass)} />
                )
              )}
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
      </TooltipProvider>
    </div>
  );
};
