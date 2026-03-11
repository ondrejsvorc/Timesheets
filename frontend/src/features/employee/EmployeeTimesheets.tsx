import { Suspense, useEffect, useMemo, useState } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Texts } from "@/constants/texts";
import { CheckCircle, XCircle, ChevronDown, Upload } from "lucide-react";
import { CZECH_MONTH_NAMES, formatMonthYear } from "@/features/contract/utils/czechMonths";
import type { EmployeeTimesheetItem, EmployeeTimesheetMonthOption, GetEmployeeTimesheetsResponse } from "./api/getEmployeeTimesheets";
import { useEmployeeTimesheetsFilter, type EmployeeTimesheetsFilterCriteria } from "./hooks/useEmployeeTimesheetsFilter";
import { UploadTimesheetsDialog } from "./UploadTimesheetsDialog";

export const EmployeeTimesheets = () => {
  const loaderData = useLoaderData() as { promise: Promise<GetEmployeeTimesheetsResponse> };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={loaderData.promise}>
        <EmployeeTimesheetsContent />
      </Await>
    </Suspense>
  );
};

const EmployeeTimesheetsContent = () => {
  const response = useAsyncValue() as GetEmployeeTimesheetsResponse;
  const { filter, setFilter } = useEmployeeTimesheetsFilter();
  const [expandedMonths, setExpandedMonths] = useState<string[]>([]);
  const [isUploadDialogOpen, setIsUploadDialogOpen] = useState(false);
  const availableYears = response.availableYears;
  const availableMonths = useMemo(
    () => getAvailableMonthsForYear(response.availableMonths, filter.year, filter.onlyUnapproved),
    [response.availableMonths, filter.year, filter.onlyUnapproved],
  );

  const handleUploadClick = () => {
    setIsUploadDialogOpen(true);
  };

  useEffect(() => {
    const fallbackYear = getFallbackYear(availableYears, filter.year);
    const yearToUse = fallbackYear ?? filter.year;
    const nextAvailableMonthSet = new Set(getAvailableMonthsForYear(response.availableMonths, yearToUse, filter.onlyUnapproved).map((item) => item.month));

    if (filter.months === null) {
      if (fallbackYear !== null) {
        setFilter((draft) => {
          draft.year = fallbackYear;
        });
      }
      return;
    }

    const nextMonths = filter.months.filter((month) => nextAvailableMonthSet.has(month));
    const monthsChanged = nextMonths.length !== filter.months.length;

    if (fallbackYear !== null || monthsChanged) {
      setFilter((draft) => {
        if (fallbackYear !== null) {
          draft.year = fallbackYear;
        }
        if (monthsChanged) {
          draft.months = nextMonths.length > 0 ? nextMonths : null;
        }
      });
    }
  }, [availableYears, filter.months, filter.onlyUnapproved, filter.year, response.availableMonths, setFilter]);

  const groupedByMonth = groupTimesheetsByMonth(response.timesheets, filter);

  let filteredMonths = filter.onlyUnapproved
    ? groupedByMonth.filter((month) => !month.allApproved)
    : groupedByMonth;

  if (filter.months !== null && filter.months.length > 0) {
    filteredMonths = filteredMonths.filter((month) => filter.months!.includes(month.month));
  }

  if (filteredMonths.length === 0) {
    return (
      <>
        <SubPageHeader>
          <SubPageTitle>Výkazy</SubPageTitle>
        </SubPageHeader>
        <FilterBar
          filter={filter}
          setFilter={setFilter}
          actions={
            <Button variant="outline" onClick={handleUploadClick}>
              <Upload className="mr-2 h-4 w-4" />
              {Texts.importFiles}
            </Button>
          }
        >
          <EmployeeTimesheetsFilterControls availableYears={availableYears} availableMonths={availableMonths} />
        </FilterBar>
        <EmptyState />
        {isUploadDialogOpen && <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => {}} />}
      </>
    );
  }

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>Výkazy</SubPageTitle>
      </SubPageHeader>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={
          <Button variant="outline" onClick={handleUploadClick}>
            <Upload className="mr-2 h-4 w-4" />
            {Texts.importFiles}
          </Button>
        }
      >
        <EmployeeTimesheetsFilterControls availableYears={availableYears} availableMonths={availableMonths} />
      </FilterBar>
      <Accordion type="multiple" value={expandedMonths} onValueChange={setExpandedMonths} className="space-y-2">
        {filteredMonths.map((monthGroup) => {
          const monthKey = `${monthGroup.year}-${monthGroup.month}`;
          const approvedCount = monthGroup.timesheets.filter((t) => t.status === Texts.statusApproved).length;
          const totalCount = monthGroup.timesheets.length;

          return (
            <AccordionItem key={monthKey} value={monthKey} className="rounded-md border !border-b">
              <AccordionTrigger className="px-4 cursor-pointer">
                <div className="flex items-center gap-3 flex-1">
                  {monthGroup.allApproved ? (
                    <CheckCircle className="size-5 text-green-600 shrink-0" aria-hidden />
                  ) : (
                    <XCircle className="size-5 text-destructive shrink-0" aria-hidden />
                  )}
                  <span className="font-medium">{formatMonthYear(monthGroup.month, monthGroup.year)}</span>
                  <span className="text-muted-foreground text-sm ml-auto">
                    Schválený: {approvedCount}/{totalCount}
                  </span>
                </div>
              </AccordionTrigger>
              <AccordionContent>
                <div className="px-4 pb-4 space-y-4">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Výkaz</TableHead>
                        <TableHead>Stav</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {monthGroup.timesheets.map((timesheet) => (
                        <TableRow key={timesheet.id} className="cursor-pointer hover:bg-muted/50">
                          <TableCell>{timesheet.contractName}</TableCell>
                          <TableCell>{timesheet.status}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  <div className="flex gap-2 pt-2">
                    <Button variant="outline" size="sm">Spravovat výkazy</Button>
                  </div>
                </div>
              </AccordionContent>
            </AccordionItem>
          );
        })}
      </Accordion>
      {isUploadDialogOpen && <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => {}} />}
    </>
  );
};

interface EmployeeTimesheetsFilterControlsProps {
  availableYears: number[];
  availableMonths: EmployeeTimesheetMonthOption[];
}

function EmployeeTimesheetsFilterControls({ availableYears, availableMonths }: EmployeeTimesheetsFilterControlsProps) {
  const { filter, setFilter } = useFilterContext<EmployeeTimesheetsFilterCriteria>();
  const [monthPopoverOpen, setMonthPopoverOpen] = useState(false);
  const selectedYear = availableYears.includes(filter.year) ? String(filter.year) : undefined;

  const handleMonthToggle = (month: number | null) => {
    setFilter((draft) => {
      if (month === null) {
        // "All months" selected - clear individual selections
        draft.months = null;
      } else {
        // Individual month selected
        if (draft.months === null) {
          // Currently "all months" - switch to array with this month
          draft.months = [month];
        } else {
          // Toggle this month in the array
          if (draft.months.includes(month)) {
            draft.months = draft.months.filter((m) => m !== month);
            // If no months selected, set to null (all months)
            if (draft.months.length === 0) {
              draft.months = null;
            }
          } else {
            draft.months = [...draft.months, month];
          }
        }
      }
    });
  };

  const isAllMonthsSelected = filter.months === null;
  const selectedMonthsCount = filter.months?.length ?? 0;

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">Rok</Label>
        <Select
          value={selectedYear}
          disabled={availableYears.length === 0}
          onValueChange={(v) =>
            setFilter((draft) => {
              draft.year = parseInt(v, 10);
            })
          }
        >
          <SelectTrigger className="w-[100px]">
            <SelectValue placeholder={Texts.noItems} />
          </SelectTrigger>
          <SelectContent>
            {availableYears.map((y) => (
              <SelectItem key={y} value={String(y)}>
                {y}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label className="text-muted-foreground text-sm">Měsíc</Label>
        <Popover open={monthPopoverOpen} onOpenChange={setMonthPopoverOpen}>
          <PopoverTrigger asChild>
            <Button variant="outline" className="w-[180px] justify-between" disabled={availableMonths.length === 0}>
              <span className="truncate">
                {availableMonths.length === 0
                  ? Texts.noItems
                  : isAllMonthsSelected
                  ? "Všechny měsíce"
                  : selectedMonthsCount === 0
                    ? "Vyberte měsíce"
                    : selectedMonthsCount === 1
                      ? CZECH_MONTH_NAMES[filter.months![0]!]
                      : `${selectedMonthsCount} měsíců`}
              </span>
              <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-[200px] p-2" align="start">
            <div className="space-y-1">
              <div
                className="flex items-center space-x-2 rounded-sm px-2 py-1.5 hover:bg-accent cursor-pointer"
                onClick={() => handleMonthToggle(null)}
              >
                <Checkbox checked={isAllMonthsSelected} />
                <Label className="text-sm font-normal cursor-pointer">Všechny měsíce</Label>
              </div>
              <div className="border-t my-1" />
              {availableMonths.map(({ month }) => {
                const isSelected = filter.months?.includes(month) ?? false;
                return (
                  <div
                    key={month}
                    className="flex items-center space-x-2 rounded-sm px-2 py-1.5 hover:bg-accent cursor-pointer"
                    onClick={() => handleMonthToggle(month)}
                  >
                    <Checkbox checked={isSelected} />
                    <Label className="text-sm font-normal cursor-pointer">{CZECH_MONTH_NAMES[month]}</Label>
                  </div>
                );
              })}
            </div>
          </PopoverContent>
        </Popover>
      </div>
      <div className="flex items-center gap-3 pt-6">
        <Checkbox
          id="only-unapproved"
          checked={filter.onlyUnapproved}
          onCheckedChange={(checked) =>
            setFilter((draft) => {
              draft.onlyUnapproved = checked === true;
            })
          }
        />
        <Label htmlFor="only-unapproved" className="text-sm cursor-pointer">
          Pouze měsíce s neschválenými výkazy
        </Label>
      </div>
    </>
  );
}

interface MonthGroup {
  year: number;
  month: number;
  timesheets: EmployeeTimesheetItem[];
  allApproved: boolean;
}

function getAvailableMonthsForYear(
  availableMonths: EmployeeTimesheetMonthOption[],
  year: number,
  onlyUnapproved: boolean,
) {
  return availableMonths.filter((item) => item.year === year && (!onlyUnapproved || item.hasUnapproved));
}

function getFallbackYear(availableYears: number[], selectedYear: number) {
  if (availableYears.length === 0 || availableYears.includes(selectedYear)) {
    return null;
  }

  const currentYear = new Date().getFullYear();
  return availableYears.includes(currentYear) ? currentYear : availableYears[availableYears.length - 1]!;
}

function groupTimesheetsByMonth(
  timesheets: EmployeeTimesheetItem[],
  filter: EmployeeTimesheetsFilterCriteria,
): MonthGroup[] {
  // Filter by year
  const filtered = timesheets.filter((t) => t.year === filter.year);

  // Group by month
  const byMonth = new Map<string, EmployeeTimesheetItem[]>();
  for (const t of filtered) {
    const key = `${t.year}-${t.month}`;
    const list = byMonth.get(key) ?? [];
    list.push(t);
    byMonth.set(key, list);
  }

  // Convert to array and sort
  const groups: MonthGroup[] = Array.from(byMonth.entries()).map(([key, items]) => {
    const [y, m] = key.split("-").map(Number) as [number, number];
    const allApproved = items.every((t) => t.status === Texts.statusApproved);
    return {
      year: y,
      month: m,
      timesheets: items.sort((a, b) => a.contractName.localeCompare(b.contractName)),
      allApproved,
    };
  });

  return groups.sort((a, b) => (a.year !== b.year ? a.year - b.year : a.month - b.month));
}
