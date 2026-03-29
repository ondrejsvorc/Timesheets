import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import { HoursToHumanTooltip } from "@/components/shared/tooltips/HoursToHumanTooltip";
import type { EditableFieldProps } from "./FieldProps";

interface ProjectFieldProps extends EditableFieldProps<number | null> {
  locked?: boolean;
}

export const Project = ({ value, onChange, locked = false }: ProjectFieldProps) => {
  return (
    <HoursToHumanTooltip hours={value ?? 0}>
      <SmartDecimalInput
        value={value}
        onChange={onChange}
        commitOnChange
        precision={2}
        disabled={locked}
        className="h-8 w-20 max-w-full text-right tabular-nums"
      />
    </HoursToHumanTooltip>
  );
};