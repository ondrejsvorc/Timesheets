import { SmartDecimalInput } from "@/components/shared/inputs/SmartDecimalInput";
import type { EditableFieldProps } from "./FieldProps";

export const Project = ({ value, onChange }: EditableFieldProps<number | null>) => {
  return (
    <SmartDecimalInput
      value={value}
      onChange={onChange}
      className="h-8 w-20 max-w-full text-right tabular-nums"
    />
  );
};