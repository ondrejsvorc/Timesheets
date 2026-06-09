import type { GetTimesheetCatalogResponse } from "./getTimesheetCatalog";
import { getTimesheetCatalog } from "./getTimesheetCatalog";
import type { GetTimesheetStatusesResponse } from "./getTimesheetStatuses";
import { getTimesheetStatuses } from "./getTimesheetStatuses";

export type ChangeTimesheetStatusOptions = GetTimesheetStatusesResponse & GetTimesheetCatalogResponse;

export const getChangeTimesheetStatusOptions = (employeeId: string, year: number, month: number) => ({
  promise: Promise.all([getTimesheetStatuses().promise, getTimesheetCatalog(employeeId, year, month).promise]).then(
    ([statusesResponse, catalogResponse]) => ({
      ...statusesResponse,
      ...catalogResponse,
    }),
  ),
});
