import { EmployeeTypeAcademicId } from "@/constants/api";
import { Texts } from "@/constants/texts";
import { compareIds } from "./compareIds";

export const resolveEmployeeTypeName = (employeeTypeId: string | null): string => {
  if (employeeTypeId == null) {
    return "";
  }

  return compareIds(employeeTypeId, EmployeeTypeAcademicId) ? Texts.academic : Texts.nonAcademic;
};
