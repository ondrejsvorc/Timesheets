import type { EditableFieldProps } from "./FieldProps";
import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";

export const CoreEmployment = ({ value, onChange }: EditableFieldProps<number | null>) => {
  return (
    <HoursToHumanTooltip hours={value ?? 0}>
      <SmartDecimalInput
        value={value}
        onChange={onChange}
        commitOnChange
        className="h-8 w-20 max-w-full text-right tabular-nums"
      />
    </HoursToHumanTooltip>
  );
};