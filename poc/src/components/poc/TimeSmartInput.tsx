import { useEffect, useState } from "react";
import { Input } from "../ui/input";

interface TimeSmartInputProps {
  value: string;
  onChange: (formattedValue: string) => void;
}

export const TimeSmartInput = ({ value, onChange }: TimeSmartInputProps) => {
  const [localValue, setLocalValue] = useState(value);

  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  const commitChange = () => {
    const formatted = formatSmartTime(localValue);
    setLocalValue(formatted);
    onChange(formatted);
  };

  return (
    <Input
      type="text"
      className="h-8 w-16"
      value={localValue}
      onFocus={(e) => e.target.select()}
      onChange={(e) => setLocalValue(e.target.value)}
      onBlur={commitChange}
      onKeyDown={(e) => {
        if (e.key === "Enter") {
          commitChange();
          (e.target as HTMLInputElement).blur();
        }
      }}
    />
  );
};

const formatSmartTime = (value: string): string => {
  const clean = value.replace(/\D/g, "");
  if (!clean) return "";

  let hours = 0;
  let minutes = 0;

  if (clean.length <= 2) {
    // Vstup "8" -> 08:00, "12" -> 12:00
    hours = parseInt(clean);
  } else {
    // Vstup "1230" -> 12:30, "815" -> 08:15
    hours = parseInt(clean.slice(0, -2));
    minutes = parseInt(clean.slice(-2));
  }

  const h = Math.min(hours, 23);
  const m = Math.min(minutes, 59);

  return `${h.toString().padStart(2, "0")}:${m.toString().padStart(2, "0")}`;
};
