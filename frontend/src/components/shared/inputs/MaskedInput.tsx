import type * as React from "react";
import { Input } from "@/components/ui/input";

type MaskedInputProps = Omit<React.ComponentProps<typeof Input>, "onChange" | "value"> & {
  value?: string;
  mask: (value: string) => string;
  onChange?: (value: string) => void;
};

const maskGroups = (value: string, groups: number[], separator: string) => {
  const digits = value.replace(/\D/g, "").slice(
    0,
    groups.reduce((sum, group) => sum + group, 0),
  );
  const parts: string[] = [];
  let offset = 0;

  for (const group of groups) {
    const part = digits.slice(offset, offset + group);
    if (!part) break;
    parts.push(part);
    offset += group;
  }

  const masked = parts.join(separator);
  const lastGroupComplete = groups.slice(0, parts.length).reduce((sum, group) => sum + group, 0) === digits.length;
  return /\D$/.test(value) && lastGroupComplete && digits.length < groups.reduce((sum, group) => sum + group, 0) ? `${masked}${separator}` : masked;
};

export const maskContractRegistrationNumber = (value: string) => maskGroups(value, [5, 2, 4, 2], " ");
export const maskPositionCode = (value: string) => maskGroups(value, [1, 1, 1, 1, 3], ".");
export const contractRegistrationNumberPattern = /^\d{5} \d{2} \d{4} \d{2}$/;
export const positionCodePattern = /^\d\.\d\.\d\.\d\.\d{3}$/;

export const MaskedInput = ({ value, mask, onChange, ...props }: MaskedInputProps) => {
  return <Input {...props} value={mask(value ?? "")} onChange={(event) => onChange?.(mask(event.currentTarget.value))} />;
};
