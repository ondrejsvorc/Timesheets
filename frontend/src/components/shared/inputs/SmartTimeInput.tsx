import { startTransition, useEffect, useState } from "react";
import { Input } from "../../ui/input";

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

interface SmartTimeInputProps {
  value: string;
  onChange: (formattedValue: string) => void;
}

export const SmartTimeInput = ({ value, onChange }: SmartTimeInputProps) => {
  const [draft, setDraft] = useState<string>(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  const commit = () => {
    const formatted = formatSmartTime(draft);
    setDraft(formatted);
    startTransition(() => {
      onChange(formatted);
    });
  };

  return (
    <Input
      type="text"
      className="h-8 w-16"
      placeholder="00:00"
      value={draft}
      onFocus={(e) => e.currentTarget.select()}
      onChange={(e) => setDraft(e.currentTarget.value)}
      onBlur={commit}
      onKeyDown={(e) => {
        if (e.key === "Enter") {
          commit();
          e.currentTarget.blur();
        }
      }}
    />
  );
};


