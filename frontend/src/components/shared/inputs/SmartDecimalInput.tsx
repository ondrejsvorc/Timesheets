import { useEffect, useRef, useState } from "react";
import { Input } from "@/components/ui/input";

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
  value: number | null
  onChange: (value: number | null) => void
  precision?: number
  className?: string
}

export const SmartDecimalInput = ({value, onChange, precision = 2, className}: SmartDecimalInputProps) => {
  const [displayValue, setDisplayValue] = useState(() =>
    formatDecimalForDisplay(value, precision)
  );

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

    if (normalizedValue === "") {
      onChange(0);
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
    onChange(rounded);
  };

  const handleBlur = () => {
    if (displayValue === "") {
      onChange(0);
      return;
    }

    const parsed = parseDecimalFromDisplay(displayValue);

    if (Number.isNaN(parsed)) {
      const revert = valueBeforeEditRef.current;
      setDisplayValue(formatDecimalForDisplay(revert, precision));
      onChange(revert ?? 0);
      return;
    }

    const rounded = Number(parsed.toFixed(precision));

    if (!isValidHours(rounded)) {
      const revert = valueBeforeEditRef.current;
      setDisplayValue(formatDecimalForDisplay(revert, precision));
      onChange(revert ?? 0);
      return;
    }

    setDisplayValue(formatDecimalForDisplay(rounded, precision));

    if (rounded !== value) {
      onChange(rounded);
    }
  };

  return (
    <Input
      ref={inputRef}
      type="text"
      inputMode="decimal"
      value={displayValue}
      onFocus={handleFocus}
      onChange={handleChange}
      onBlur={handleBlur}
      className={className}
      maxLength={5}
    />
  );
};