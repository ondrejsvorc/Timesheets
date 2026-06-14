import { startTransition, useEffect, useRef, useState } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/utils/cn";

const DECIMAL_PATTERN = /^\d*(,\d*)?$/;

const formatDecimalForDisplay = (value: number | null, precision: number) => {
  if (value === null || value === 0) return "";
  const rounded = Number(value.toFixed(precision));
  return rounded.toString().replace(".", ",");
};

const parseDecimalFromDisplay = (displayValue: string) => parseFloat(displayValue.replace(",", "."));
const isValidDecimalInput = (displayValue: string) => DECIMAL_PATTERN.test(displayValue);
const isValidHours = (value: number) => value >= 0 && value <= 24;

interface SmartDecimalInputProps {
  value: number | null;
  onChange: (value: number | null) => void;
  precision?: number;
  commitOnChange?: boolean;
  className?: string;
  disabled?: boolean;
}

export const SmartDecimalInput = ({ value, onChange, precision = 3, commitOnChange = false, className, disabled = false }: SmartDecimalInputProps) => {
  const [displayValue, setDisplayValue] = useState(() => formatDecimalForDisplay(value, precision));

  const valueBeforeEditRef = useRef<number | null>(value);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const formatted = formatDecimalForDisplay(value, precision);
    setDisplayValue(formatted);
  }, [value, precision]);

  const handleFocus = () => {
    valueBeforeEditRef.current = value;
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const input = event.target;
    const caretPosition = input.selectionStart;

    const normalizedValue = input.value.replace(".", ",");
    if (normalizedValue !== "" && !isValidDecimalInput(normalizedValue)) {
      return;
    }

    setDisplayValue(normalizedValue);

    requestAnimationFrame(() => {
      if (inputRef.current && caretPosition !== null) {
        inputRef.current.setSelectionRange(caretPosition, caretPosition);
      }
    });

    if (!commitOnChange) {
      return;
    }

    if (normalizedValue === "") {
      startTransition(() => {
        onChange(0);
      });
      return;
    }

    if (normalizedValue.endsWith(",")) {
      return;
    }

    const parsed = parseDecimalFromDisplay(normalizedValue);
    if (Number.isNaN(parsed)) {
      return;
    }

    const rounded = Number(parsed.toFixed(precision));
    if (!isValidHours(rounded)) {
      return;
    }

    startTransition(() => {
      onChange(rounded);
    });
  };

  const commit = () => {
    if (displayValue === "") {
      startTransition(() => {
        onChange(0);
      });
      return;
    }

    const parsed = parseDecimalFromDisplay(displayValue);

    if (Number.isNaN(parsed)) {
      const revert = valueBeforeEditRef.current;
      setDisplayValue(formatDecimalForDisplay(revert, precision));
      startTransition(() => {
        onChange(revert ?? 0);
      });
      return;
    }

    const rounded = Number(parsed.toFixed(precision));

    if (!isValidHours(rounded)) {
      const revert = valueBeforeEditRef.current;
      setDisplayValue(formatDecimalForDisplay(revert, precision));
      startTransition(() => {
        onChange(revert ?? 0);
      });
      return;
    }

    setDisplayValue(formatDecimalForDisplay(rounded, precision));

    if (rounded !== value) {
      startTransition(() => {
        onChange(rounded);
      });
    }
  };

  return (
    <Input
      ref={inputRef}
      type="text"
      inputMode="decimal"
      value={disabled ? formatDecimalForDisplay(value, precision) : displayValue}
      onFocus={handleFocus}
      onChange={handleChange}
      onBlur={commit}
      onKeyDown={(e) => {
        if (e.key === "Enter") {
          commit();
          e.currentTarget.blur();
        }
      }}
      disabled={disabled}
      className={cn("tabular-nums", disabled && "cursor-not-allowed border-dashed border-slate-300 bg-slate-100/80 text-slate-600 opacity-100", className)}
      maxLength={6}
    />
  );
};
