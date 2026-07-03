import type * as React from "react";
import { Input } from "@/components/ui/input";

type MaskedInputProps = Omit<React.ComponentProps<typeof Input>, "onChange" | "value"> & {
  value?: string;
  mask: (value: string) => string;
  onChange?: (value: string) => void;
};

export const maskGroups = (value: string, groups: number[], separator: string) => {
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

const DATE_PART_SEP = ". ";

type DatePart = { value: string; digitsRead: number; isComplete: boolean; autoSkip: boolean };

const parseDayPart = (digits: string): DatePart => {
  if (!digits) return { value: "", digitsRead: 0, isComplete: false, autoSkip: false };

  const firstDigit = digits[0];
  if (firstDigit >= "4" && firstDigit <= "9") {
    return { value: firstDigit, digitsRead: 1, isComplete: true, autoSkip: true };
  }

  if (digits.length === 1) {
    return { value: firstDigit, digitsRead: 1, isComplete: false, autoSkip: false };
  }

  const secondDigit = digits[1];
  if (firstDigit === "3" && secondDigit > "1") {
    return { value: "3", digitsRead: 1, isComplete: false, autoSkip: false };
  }

  const day = Number(`${firstDigit}${secondDigit}`);
  if (day === 0) {
    return { value: "1", digitsRead: 2, isComplete: true, autoSkip: false };
  }
  if (day > 31) {
    return { value: firstDigit, digitsRead: 1, isComplete: true, autoSkip: true };
  }

  return { value: String(day), digitsRead: 2, isComplete: true, autoSkip: false };
};

const parseMonthPart = (digits: string): DatePart => {
  if (!digits) return { value: "", digitsRead: 0, isComplete: false, autoSkip: false };

  const firstDigit = digits[0];
  if (firstDigit >= "3" && firstDigit <= "9") {
    return { value: firstDigit, digitsRead: 1, isComplete: true, autoSkip: true };
  }
  if (firstDigit === "2") {
    return { value: "2", digitsRead: 1, isComplete: true, autoSkip: true };
  }

  if (digits.length === 1) {
    return { value: firstDigit, digitsRead: 1, isComplete: false, autoSkip: false };
  }

  const secondDigit = digits[1];
  if (firstDigit === "1" && secondDigit > "2") {
    return { value: "1", digitsRead: 1, isComplete: false, autoSkip: false };
  }

  const month = Number(`${firstDigit}${secondDigit}`);
  if (month === 0) {
    return { value: "1", digitsRead: 2, isComplete: true, autoSkip: false };
  }
  if (month > 12) {
    return { value: firstDigit, digitsRead: 1, isComplete: true, autoSkip: true };
  }

  return { value: String(month), digitsRead: 2, isComplete: true, autoSkip: false };
};

const trailingSeparator = (part: DatePart) => (part.autoSkip ? DATE_PART_SEP : "");

const formatYearPart = (digits: string): { value: string; digitsRead: number } => {
  if (!digits) return { value: "", digitsRead: 0 };
  if (digits.length === 1) {
    return { value: digits === "2" ? "20" : digits, digitsRead: 1 };
  }
  const digitsRead = Math.min(digits.length, 4);
  return { value: digits.slice(0, 4), digitsRead };
};

const preserveTrailingSeparator = (value: string, formatted: string) => {
  const trimmed = value.trimEnd();
  if ((trimmed.endsWith(".") || trimmed.endsWith(DATE_PART_SEP)) && !formatted.endsWith(DATE_PART_SEP)) {
    return `${formatted}${DATE_PART_SEP}`;
  }
  return formatted;
};

export const maskSmartDate = (value: string): string => {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  if (!digits) return "";

  const day = parseDayPart(digits);
  let offset = day.digitsRead;

  if (offset >= digits.length) {
    return preserveTrailingSeparator(value, `${day.value}${trailingSeparator(day)}`);
  }

  const month = parseMonthPart(digits.slice(offset));
  offset += month.digitsRead;

  if (offset >= digits.length) {
    return preserveTrailingSeparator(value, `${day.value}${DATE_PART_SEP}${month.value}${trailingSeparator(month)}`);
  }

  const year = formatYearPart(digits.slice(offset));
  return `${day.value}${DATE_PART_SEP}${month.value}${DATE_PART_SEP}${year.value}`;
};

export const maskContractRegistrationNumber = (value: string) => maskGroups(value, [5, 2, 4, 2], " ");
export const maskDate = maskSmartDate;
export const contractRegistrationNumberPattern = /^\d{5} \d{2} \d{4} \d{2}$/;

export const MaskedInput = ({ value, mask, onChange, ...props }: MaskedInputProps) => {
  return <Input {...props} value={mask(value ?? "")} onChange={(event) => onChange?.(mask(event.currentTarget.value))} />;
};
