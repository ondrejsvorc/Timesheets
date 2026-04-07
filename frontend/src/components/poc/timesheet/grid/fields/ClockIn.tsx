import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";
import type { EditableFieldProps } from "./FieldProps";

export const ClockIn = ({ value, onChange }: EditableFieldProps<string>) => {
  return <SmartTimeInput value={value} onChange={onChange} />;
};
