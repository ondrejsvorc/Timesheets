import { SmartTimeInput } from "@/components/shared/inputs/SmartTimeInput";
import type { EditableFieldProps } from "./FieldProps";

export const ClockOut = ({ value, onChange }: EditableFieldProps<string>) => {
  return <SmartTimeInput value={value} onChange={onChange} />;
};
