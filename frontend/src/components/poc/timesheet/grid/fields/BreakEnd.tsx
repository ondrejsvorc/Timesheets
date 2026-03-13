import type { EditableFieldProps } from "./FieldProps";
import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";

export const BreakEnd = ({ value, onChange }: EditableFieldProps<string>) => {
  return (
    <SmartTimeInput value={value} onChange={onChange} />
  );
};