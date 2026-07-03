import { cs } from "date-fns/locale";
import { CalendarIcon, X } from "lucide-react";
import { useCallback, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { cn } from "@/utils/common";
import { formatDate, fromDateOnlyIso, toDateOnlyIso } from "@/utils/format";

interface DatePickerProps {
  value?: string | null;
  disabled?: boolean;
  clearable?: boolean;
  disabledDate?: (date: Date) => boolean;
  onChange: (nextValue: string | undefined) => void;
}

export const DatePicker = ({ value, disabled, clearable = true, disabledDate, onChange }: DatePickerProps) => {
  const [open, setOpen] = useState(false);
  const selected = useMemo(() => (value ? fromDateOnlyIso(value) : undefined), [value]);
  const label = formatDate(value);

  const handleSelect = useCallback(
    (date: Date | undefined) => {
      if (!date) {
        onChange(undefined);
        setOpen(false);
        return;
      }
      if (value) {
        const current = fromDateOnlyIso(value);
        if (current.toDateString() === date.toDateString()) {
          return;
        }
      }
      onChange(toDateOnlyIso(date));
      setOpen(false);
    },
    [onChange, value],
  );

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button type="button" variant="outline" disabled={disabled} className={cn("w-full pl-3 text-left font-normal flex items-center gap-2", !value && "text-muted-foreground")}>
          <span className="flex-1 truncate">{label}</span>
          {clearable && value && !disabled ? (
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-sm p-1 hover:bg-muted"
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onChange(undefined);
                setOpen(false);
              }}
              aria-label="Vymazat datum"
            >
              <X className="h-4 w-4 opacity-60" />
            </button>
          ) : (
            <CalendarIcon className="h-4 w-4 opacity-50" />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar mode="single" selected={selected} onSelect={handleSelect} disabled={disabledDate} locale={cs} />
      </PopoverContent>
    </Popover>
  );
};
