import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { ActionDropdownMenu, EditAction } from "@/components/shared/menus/ActionDropdownMenu";
import { MultiSelectComboBox, type MultiSelectComboBoxItem } from "@/components/shared/inputs/MultiSelectComboBox";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { CheckCircle, XCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { useParams } from "react-router";
import type { EmployeeGroupView, MonthGroupView, TimesheetRowView } from "./api/getContractTimesheets";
import { buildEmployeesView, buildMonthsView } from "./api/getContractTimesheets";
import type { GetContractTimesheetsFilterOptionsResponse } from "./api/getContractTimesheetsFilterOptions";
import { getContractTimesheetsFilterOptions } from "./api/getContractTimesheetsFilterOptions";
import { CZECH_MONTH_NAMES, formatMonthYear } from "./utils/czechMonths";
import {
  type ContractTimesheetsFilterCriteria,
  useContractTimesheetsFilter,
} from "./hooks/useContractTimesheetsFilter";
import { useContractTimesheets } from "./hooks/useContractTimesheets";

export const ContractTimesheets = () => {
  const { id: projectId, contractId } = useParams<{ id: string; contractId: string }>();
  const { filter, setFilter } = useContractTimesheetsFilter();
  const { data, isLoading, fetchTimesheets } = useContractTimesheets(projectId ?? "", contractId ?? "");
  const [filterOptions, setFilterOptions] = useState<GetContractTimesheetsFilterOptionsResponse | null>(null);

  useEffect(() => {
    if (projectId && contractId) {
      fetchTimesheets(filter);
    }
  }, [projectId, contractId]); // eslint-disable-line react-hooks/exhaustive-deps -- initial fetch only

  useEffect(() => {
    let cancelled = false;
    if (!contractId) return;

    getContractTimesheetsFilterOptions(contractId).then((res) => {
      if (!cancelled) setFilterOptions(res);
    });

    return () => {
      cancelled = true;
    };
  }, [contractId]);

  useEffect(() => {
    if (!filterOptions?.years.length || !filterOptions.months.length) return;

    const years = filterOptions.years;
    const months = filterOptions.months;
    const minYear = years[0]!;
    const maxYear = years[years.length - 1]!;
    const minMonth = months[0]!;
    const maxMonth = months[months.length - 1]!;

    const clampYear = (y: number) => (years.includes(y) ? y : y < minYear ? minYear : maxYear);
    const clampMonth = (m: number) => (months.includes(m) ? m : m < minMonth ? minMonth : maxMonth);

    const nextFromYear = clampYear(filter.fromYear);
    const nextToYear = clampYear(filter.toYear);
    const nextFromMonth = clampMonth(filter.fromMonth);
    const nextToMonth = clampMonth(filter.toMonth);

    const invalidRange =
      nextFromYear > nextToYear || (nextFromYear === nextToYear && nextFromMonth > nextToMonth);

    if (
      nextFromYear !== filter.fromYear ||
      nextToYear !== filter.toYear ||
      nextFromMonth !== filter.fromMonth ||
      nextToMonth !== filter.toMonth ||
      invalidRange
    ) {
      setFilter((draft) => {
        draft.fromYear = nextFromYear;
        draft.fromMonth = nextFromMonth;
        draft.toYear = nextToYear;
        draft.toMonth = nextToMonth;

        if (invalidRange) {
          draft.fromYear = minYear;
          draft.fromMonth = minMonth;
          draft.toYear = maxYear;
          draft.toMonth = maxMonth;
        }
      });
    }
  }, [filter.fromMonth, filter.fromYear, filter.toMonth, filter.toYear, filterOptions, setFilter]);

  const handleFilter = () => {
    fetchTimesheets(filter);
  };

  const monthsView = data ? buildMonthsView(data) : [];
  const employeesView = data ? buildEmployeesView(data) : [];
  const contentReady = !isLoading || data != null;

  return (
    <>
      {!contentReady ? (
        <GenericSkeleton />
      ) : (
        <>
          <SubPageHeader>
            <SubPageTitle>Výkazy</SubPageTitle>
          </SubPageHeader>
          <FilterBar filter={filter} setFilter={setFilter} actions={<Button onClick={handleFilter}>Filtrovat</Button>}>
            <ContractTimesheetsFilterControls options={filterOptions} />
          </FilterBar>
          {filter.groupBy === "Month" ? (
            <TimesheetsByMonth
              months={monthsView}
              isLoading={isLoading}
            />
          ) : (
            <TimesheetsByEmployee
              employees={employeesView}
              isLoading={isLoading}
            />
          )}
        </>
      )}
    </>
  );
};

function ContractTimesheetsFilterControls({ options }: { options: GetContractTimesheetsFilterOptionsResponse | null }) {
  const { filter, setFilter } = useFilterContext<ContractTimesheetsFilterCriteria>();
  const yearOptions = options?.years ?? [];
  const monthOptions = options?.months ?? [];
  const statusOptions: MultiSelectComboBoxItem[] = (options?.statuses ?? []).map((s) => ({ value: s, label: s }));

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.grouping}</Label>
        <Select
          value={filter.groupBy}
          onValueChange={(v: "Employee" | "Month") =>
            setFilter((draft) => {
              draft.groupBy = v;
            })
          }
        >
          <SelectTrigger className="w-[220px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Employee">{Texts.groupByEmployee}</SelectItem>
            <SelectItem value="Month">{Texts.groupByMonthAndEmployee}</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.periodFrom}</Label>
        <div className="flex gap-2">
        <Select
          value={String(filter.fromYear)}
          disabled={yearOptions.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.fromYear = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[88px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {yearOptions.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={String(filter.fromMonth)}
          disabled={monthOptions.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.fromMonth = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {monthOptions.map((m) => (
              <SelectItem key={m} value={String(m)}>
                {CZECH_MONTH_NAMES[m]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.periodTo}</Label>
        <div className="flex gap-2">
        <Select
          value={String(filter.toYear)}
          disabled={yearOptions.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.toYear = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[88px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {yearOptions.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={String(filter.toMonth)}
          disabled={monthOptions.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.toMonth = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {monthOptions.map((m) => (
              <SelectItem key={m} value={String(m)}>
                {CZECH_MONTH_NAMES[m]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">{Texts.status}</Label>
        <MultiSelectComboBox
          value={filter.statuses ?? []}
          items={statusOptions}
          placeholder={Texts.status}
          maxVisibleItems={2}
          onChange={(value) =>
            setFilter((draft) => {
              draft.statuses = value.length ? value : undefined;
            })
          }
        />
      </div>
    </>
  );
}

function workloadPercent(workload: number): string {
  return `${Math.round(workload * 100)}%`;
}

function ApprovalIcon({ approved }: { approved: boolean }) {
  return approved ? (
    <CheckCircle className="size-5 text-green-600" aria-hidden />
  ) : (
    <XCircle className="size-5 text-destructive" aria-hidden />
  );
}

interface TimesheetsByMonthProps {
  months: MonthGroupView[];
  isLoading: boolean;
}

const TimesheetsByMonth = ({ months, isLoading }: TimesheetsByMonthProps) => {
  if (months.length === 0) {
    return isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {months.map((monthGroup) => (
        <div key={`${monthGroup.year}-${monthGroup.month}`} className="space-y-6">
          <div className="font-medium text-foreground">
            {formatMonthYear(monthGroup.month, monthGroup.year)}
          </div>
          {monthGroup.items.map((employee) => (
            <div key={employee.id} className="rounded-md border p-4">
              <div className="mb-3 flex items-center gap-2 font-medium text-foreground">
                <ApprovalIcon approved={employee.allTimesheetsApproved} />
                <span>
                  {employee.fullName} · {employee.personalNumber} · {employee.employeeType}
                </span>
              </div>
              {employee.timesheets.length === 0 ? (
                <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{Texts.position}</TableHead>
                      <TableHead>{Texts.workload}</TableHead>
                      <TableHead>{Texts.timesheetStatus}</TableHead>
                      <TableHead>{Texts.actions}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {employee.timesheets.map((item) => (
                      <TimesheetItemRow key={item.id} item={item} />
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          ))}
        </div>
      ))}
    </div>
  );
};

interface TimesheetItemRowProps {
  item: { id: string; position: string; workload: number; status: string };
}

function TimesheetItemRow({ item }: TimesheetItemRowProps) {
  return (
    <TableRow className="cursor-pointer">
      <TableCell>{item.position}</TableCell>
      <TableCell>{workloadPercent(item.workload)}</TableCell>
      <TableCell>{item.status}</TableCell>
      <TableCell>
        <ActionDropdownMenu>
          <EditAction onClick={() => {}} />
        </ActionDropdownMenu>
      </TableCell>
    </TableRow>
  );
}

/** Seskupí výkazy zaměstnance po měsících (year, month), seřazené chronologicky. */
function groupTimesheetsByMonth(timesheets: TimesheetRowView[]): { year: number; month: number; items: TimesheetRowView[] }[] {
  const withPeriod = timesheets.filter((t): t is TimesheetRowView & { year: number; month: number } => t.year != null && t.month != null);
  const byKey = new Map<string, TimesheetRowView[]>();
  for (const t of withPeriod) {
    const key = `${t.year}-${t.month}`;
    const list = byKey.get(key) ?? [];
    list.push(t);
    byKey.set(key, list);
  }
  return Array.from(byKey.entries())
    .map(([key, items]) => {
      const [y, m] = key.split("-").map(Number) as [number, number];
      return { year: y, month: m, items };
    })
    .sort((a, b) => (a.year !== b.year ? a.year - b.year : a.month - b.month));
}

interface TimesheetsByEmployeeProps {
  employees: EmployeeGroupView[];
  isLoading: boolean;
}

const TimesheetsByEmployee = ({ employees, isLoading }: TimesheetsByEmployeeProps) => {
  if (employees.length === 0) {
    return isLoading ? <GenericSkeleton /> : <EmptyState />;
  }
  return (
    <div className="space-y-8">
      {employees.map((emp) => {
        const months = groupTimesheetsByMonth(emp.timesheets);
        return (
        <div key={emp.id} className="space-y-6">
          <div className="font-medium text-foreground">
            {emp.fullName} · {emp.personalNumber} · {emp.employeeType}
          </div>
          {months.length === 0 ? (
            <p className="text-sm text-muted-foreground">{Texts.noItems}</p>
          ) : (
            months.map((monthGroup) => {
              const monthApproved = monthGroup.items.every(
                (item) => item.status === Texts.statusApproved,
              );
              return (
              <div key={`${monthGroup.year}-${monthGroup.month}`} className="rounded-md border p-4">
                <div className="mb-3 flex items-center gap-2 font-medium text-foreground">
                  <ApprovalIcon approved={monthApproved} />
                  {formatMonthYear(monthGroup.month, monthGroup.year)}
                </div>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{Texts.position}</TableHead>
                      <TableHead>{Texts.workload}</TableHead>
                      <TableHead>{Texts.timesheetStatus}</TableHead>
                      <TableHead>{Texts.actions}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {monthGroup.items.map((item) => (
                      <TimesheetItemRow key={item.id} item={item} />
                    ))}
                  </TableBody>
                </Table>
              </div>
              );
            })
          )}
        </div>
        );
      })}
    </div>
  );
};
