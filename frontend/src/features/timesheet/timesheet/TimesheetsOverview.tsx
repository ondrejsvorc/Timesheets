import { EmptyState } from "@/components/shared/data/EmptyState";
import { TimesheetStatusBadge } from "@/components/shared/data/TimesheetStatusBadge";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";
import { TimesheetOverviewRowActions } from "./TimesheetOverviewRowActions";

const formatWorkload = (value: number) =>
  `${Number((value * 100).toFixed(2))
    .toString()
    .replace(".", ",")} %`;

interface TimesheetsOverviewProps {
  overview: GetCombinedTimesheetOverviewResponse;
}

export const TimesheetsOverview = ({ overview }: TimesheetsOverviewProps) => {
  if (overview.items.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="mb-6">
      <SubPageHeader>
        <SubPageTitle>Výkazy zaměstnance</SubPageTitle>
      </SubPageHeader>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Výkaz</TableHead>
              <TableHead>{Texts.timesheetStatus}</TableHead>
              <TableHead>Zakázka</TableHead>
              <TableHead>Pozice</TableHead>
              <TableHead>Úvazek</TableHead>
              <TableHead>Manažeři zakázky</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {overview.items.map((item) => (
              <TableRow key={`${item.kind}-${item.timesheetId ?? item.label}`}>
                <TableCell className="font-medium">{item.label}</TableCell>
                <TableCell>
                  <TimesheetStatusBadge status={item.status} />
                </TableCell>
                <TableCell>{item.contractName ?? Texts.dash}</TableCell>
                <TableCell>{item.position ?? Texts.dash}</TableCell>
                <TableCell>{formatWorkload(item.workload)}</TableCell>
                <TableCell className="max-w-[20rem] whitespace-normal break-words">{item.managers.length > 0 ? item.managers.join(", ") : Texts.dash}</TableCell>
                <TableCell>
                  <TimesheetOverviewRowActions item={item} overview={overview} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
};
