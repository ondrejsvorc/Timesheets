import { X } from "lucide-react";
import type { ComponentProps } from "react";
import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { Input } from "@/components/ui/input";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/common";
import { formatDate, formatDateDisplay, isDateInRange, parseDateDisplay, toDateOnlyIso } from "@/utils/format";

type DateInputProps = Omit<ComponentProps<typeof Input>, "value" | "onChange" | "min" | "max" | "type"> & {
  value?: string | null;
  onChange: (next: string | undefined) => void;
  min?: Date;
  max?: Date;
  disabled?: boolean;
};

const formatRangeLabel = (min?: Date, max?: Date) => {
  if (!min || !max) return null;
  return Texts.dateRangeHint.replace("{min}", formatDate(toDateOnlyIso(min))).replace("{max}", formatDate(toDateOnlyIso(max)));
};

export const DateInput = ({ value, onChange, min, max, disabled, className, ...props }: DateInputProps) => {
  const errorId = useId();
  const [draft, setDraft] = useState(() => formatDateDisplay(value));
  const [touched, setTouched] = useState(false);

  useEffect(() => {
    setDraft(formatDateDisplay(value));
  }, [value]);

  const commitDraft = useCallback(
    (raw: string) => {
      const trimmed = raw.trim();
      if (!trimmed) {
        onChange(undefined);
        setDraft("");
        return;
      }
      const date = parseDateDisplay(trimmed);
      if (!date || !isDateInRange(date, min, max)) {
        return;
      }
      onChange(toDateOnlyIso(date));
      setDraft(formatDateDisplay(toDateOnlyIso(date)));
    },
    [max, min, onChange],
  );

  const handleBlur = useCallback(() => {
    setTouched(true);
    commitDraft(draft);
  }, [commitDraft, draft]);

  const validationError = useMemo(() => {
    if (!touched) return null;
    const trimmed = draft.trim();
    if (!trimmed) return null;
    const date = parseDateDisplay(trimmed);
    if (!date) return Texts.invalidDate;
    if (!isDateInRange(date, min, max)) {
      const minLabel = min ? formatDate(toDateOnlyIso(min)) : Texts.dash;
      const maxLabel = max ? formatDate(toDateOnlyIso(max)) : Texts.dash;
      return Texts.dateOutOfRange.replace("{min}", minLabel).replace("{max}", maxLabel);
    }
    return null;
  }, [draft, max, min, touched]);

  const rangeHint = useMemo(() => formatRangeLabel(min, max), [min, max]);
  const showClear = Boolean(draft) && !disabled;

  return (
    <div className="space-y-1">
      <div className="relative w-full">
        <Input
          {...props}
          type="text"
          value={draft}
          disabled={disabled}
          inputMode="numeric"
          autoComplete="off"
          aria-invalid={validationError ? true : props["aria-invalid"]}
          aria-describedby={validationError ? errorId : undefined}
          className={cn("w-full pr-9 tabular-nums", className)}
          onBlur={(event) => {
            handleBlur();
            props.onBlur?.(event);
          }}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              setTouched(true);
              commitDraft(draft);
              event.currentTarget.blur();
            }
            props.onKeyDown?.(event);
          }}
        />
        {showClear ? (
          <button
            type="button"
            className="absolute right-2 top-1/2 inline-flex -translate-y-1/2 items-center justify-center rounded-sm p-1 hover:bg-muted"
            onClick={() => {
              setDraft("");
              setTouched(false);
              onChange(undefined);
            }}
            aria-label="Vymazat datum"
          >
            <X className="h-4 w-4 opacity-60" />
          </button>
        ) : null}
      </div>
      {validationError ? (
        <p id={errorId} className="text-sm text-destructive">
          {validationError}
        </p>
      ) : rangeHint ? (
        <p className="text-xs text-muted-foreground">{rangeHint}</p>
      ) : null}
    </div>
  );
};
