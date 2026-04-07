import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";
import type { EditableFieldProps } from "./FieldProps";

export const BreakEnd = ({ value, onChange }: EditableFieldProps<string>) => {
  return <SmartTimeInput value={value} onChange={onChange} />;
};
