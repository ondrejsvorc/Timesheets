import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import type { EditableFieldProps } from "./FieldProps";

interface CoreEmploymentProps extends EditableFieldProps<number | null> {
  disabled?: boolean;
}

export const CoreEmployment = ({ value, onChange, disabled = false }: CoreEmploymentProps) => {
  return (
    <HoursToHumanTooltip hours={value ?? 0}>
      <SmartDecimalInput
        value={value}
        onChange={onChange}
        commitOnChange
        precision={2}
        disabled={disabled}
        className="h-8 w-20 max-w-full text-right tabular-nums"
      />
    </HoursToHumanTooltip>
  );
};
