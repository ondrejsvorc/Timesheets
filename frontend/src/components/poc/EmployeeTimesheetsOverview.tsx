import { useAsyncValue } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Texts } from "@/constants/texts";
import type { GetCombinedTimesheetOverviewResponse } from "./api/getCombinedTimesheetOverview";

const formatWorkload = (value: number) => `${Number((value * 100).toFixed(2)).toString().replace(".", ",")} %`;

export const EmployeeTimesheetsOverview = () => {
  const overview = useAsyncValue() as GetCombinedTimesheetOverviewResponse;

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
              <TableHead>Zakázka</TableHead>
              <TableHead>Pozice</TableHead>
              <TableHead>Úvazek</TableHead>
              <TableHead>Manažeři zakázky</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {overview.items.map((item) => (
              <TableRow key={`${item.label}-${item.contractName ?? "core"}`}>
                <TableCell className="font-medium">{item.label}</TableCell>
                <TableCell>{item.contractName ?? Texts.dash}</TableCell>
                <TableCell>{item.position ?? Texts.dash}</TableCell>
                <TableCell>{formatWorkload(item.workload)}</TableCell>
                <TableCell className="max-w-[20rem] whitespace-normal break-words">{item.managers.length > 0 ? item.managers.join(", ") : Texts.dash}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
};
