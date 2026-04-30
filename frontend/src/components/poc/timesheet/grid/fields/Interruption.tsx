import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

interface InterruptionProps {
  value: string;
}

const parseCodes = (value: string): string[] =>
  value
    .split(",")
    .map((v) => v.trim())
    .filter(Boolean);

export const Interruption = ({ value }: InterruptionProps) => {
  const codes = parseCodes(value ?? "");
  const visible = codes.slice(0, 3);
  const isTruncated = codes.length > 3;
  const label = visible.length === 0 ? "" : `${visible.join(", ")}${isTruncated ? ", …" : ""}`;

  if (!isTruncated) {
    return <span className="max-w-[11rem] truncate text-sm text-foreground/80">{label}</span>;
  }

  return (
    <Tooltip delayDuration={120}>
      <TooltipTrigger asChild>
        <span className="max-w-[11rem] truncate cursor-help text-sm text-foreground/80 border-b border-dotted border-slate-400">{label}</span>
      </TooltipTrigger>
      <TooltipContent side="top">{codes.join(", ")}</TooltipContent>
    </Tooltip>
  );
};
