import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import type { TimeRange } from "../../../Timesheet";

/** Predefined 50-minute slots from 07:00 to 21:00 (07:00–07:50, 08:00–08:50, …, 21:00–21:50). */
const STAG_SCHEDULE_INTERVAL_OPTIONS: MultiSelectComboBoxItem[] = (() => {
  const items: MultiSelectComboBoxItem[] = [];
  for (let h = 7; h <= 21; h++) {
    const start = `${h.toString().padStart(2, "0")}:00`;
    const end = `${h.toString().padStart(2, "0")}:50`;
    items.push({ value: `${start}-${end}` });
  }
  return items;
})();

const OPTION_VALUES = STAG_SCHEDULE_INTERVAL_OPTIONS.map((o) => o.value);

/** Normalize backend time (HH:mm:ss) to option format (HH:mm). */
const toOptionTime = (t: string): string => (t ? t.slice(0, 5) : "");

/** Parse HH:mm to minutes since midnight for comparison. */
const timeToMinutes = (hhmm: string): number => {
  if (!hhmm || hhmm.length < 5) return 0;
  const [h, m] = hhmm.split(":").map(Number);
  return (h ?? 0) * 60 + (m ?? 0);
};

/**
 * Rozšíří rozsah (start–end) na všechny 50min sloty, které s ním překrývají.
 * Např. 16:00–17:50 → ["16:00-16:50", "17:00-17:50"].
 */
const rangeToOptionValues = (range: TimeRange): string[] => {
  const start = toOptionTime(range.start);
  const end = toOptionTime(range.end);
  if (!start || !end) return [];
  const startMin = timeToMinutes(start);
  const endMin = timeToMinutes(end);
  if (endMin <= startMin) return [];

  return OPTION_VALUES.filter((slotValue) => {
    const [slotStart, slotEnd] = slotValue.split("-").map((s) => s ?? "");
    const slotStartMin = timeToMinutes(slotStart);
    const slotEndMin = timeToMinutes(slotEnd);
    return slotStartMin < endMin && slotEndMin > startMin;
  });
};

const fromValue = (value: string): TimeRange => {
  const [start, end] = value.split("-");
  return { start: start ?? "", end: end ?? "" };
};

interface StagScheduleProps {
  schedules: TimeRange[];
  onSchedulesChange: (schedules: TimeRange[]) => void;
  disabled?: boolean;
}

export const StagSchedule = ({ schedules, onSchedulesChange, disabled }: StagScheduleProps) => {
  const selected = [...new Set(schedules.flatMap((range) => rangeToOptionValues(range)))].sort(
    (a, b) => OPTION_VALUES.indexOf(a) - OPTION_VALUES.indexOf(b),
  );

  return (
    <div className={disabled ? "pointer-events-none opacity-50" : undefined}>
      <MultiSelectComboBox
        items={STAG_SCHEDULE_INTERVAL_OPTIONS}
        placeholder="Vyberte…"
        value={selected}
        onChange={(selectedArray) => onSchedulesChange(selectedArray.map(fromValue))}
        maxVisibleItems={1}
      />
    </div>
  );
};
