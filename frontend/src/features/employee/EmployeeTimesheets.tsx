import { CheckCircle, ChevronDown, Upload, XCircle } from "lucide-react";
import { Suspense, useEffect, useMemo, useState } from "react";
import { Await, useAsyncValue, useLoaderData, useNavigate, useRevalidator } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar, useFilterContext } from "@/components/shared/layout/FilterBar";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { CZECH_MONTH_NAMES } from "@/features/contract/utils/czechMonths";
import type { GetEmployeeTimesheetsResponse } from "./api/getEmployeeTimesheets";
import { type EmployeeTimesheetsFilterCriteria, useEmployeeTimesheetsFilter } from "./hooks/useEmployeeTimesheetsFilter";
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
  const navigate = useNavigate();
  const revalidator = useRevalidator();
  const [isUploadDialogOpen, setIsUploadDialogOpen] = useState(false);
  const availableYears = response.availableYears;
  const availableMonths = useMemo(() => getAvailableMonthsForYear(response.availableMonths, filter.year), [response.availableMonths, filter.year]);

  const handleUploadClick = () => {
    setIsUploadDialogOpen(true);
  };

  useEffect(() => {
    const fallbackYear = getFallbackYear(availableYears, filter.year);
    const yearToUse = fallbackYear ?? filter.year;
    const nextAvailableMonthSet = new Set(getAvailableMonthsForYear(response.availableMonths, yearToUse));

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
  }, [availableYears, filter.months, filter.year, response.availableMonths, setFilter]);

  let filteredMonths = response.months.filter((m) => m.year === filter.year);
  const selectedMonths = filter.months;
  if (selectedMonths !== null && selectedMonths.length > 0) filteredMonths = filteredMonths.filter((m) => selectedMonths.includes(m.month));

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
        {isUploadDialogOpen && (
          <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => revalidator.revalidate()} />
        )}
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
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-[80px]">Stav</TableHead>
              <TableHead>Měsíc</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredMonths.map((m) => {
              const disabled = !m.hasAttendanceImport;
              return (
                <TooltipProvider key={`${m.year}-${m.month}`} delayDuration={150}>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <TableRow
                        className={disabled ? "opacity-50" : "cursor-pointer hover:bg-muted/50"}
                        onClick={() => {
                          if (!disabled) navigate(Routes.timesheet(response.employeeId, m.year, m.month));
                        }}
                      >
                        <TableCell>
                          {disabled ? (
                            <XCircle className="size-5 text-destructive" aria-hidden />
                          ) : (
                            <CheckCircle className="size-5 text-green-600" aria-hidden />
                          )}
                        </TableCell>
                        <TableCell className="font-medium">
                          {CZECH_MONTH_NAMES[m.month]} {m.year}
                        </TableCell>
                      </TableRow>
                    </TooltipTrigger>
                    {disabled && <TooltipContent side="top">Nejdřív naimportujte docházku pro tento měsíc.</TooltipContent>}
                  </Tooltip>
                </TooltipProvider>
              );
            })}
          </TableBody>
        </Table>
      </div>
      {isUploadDialogOpen && (
        <UploadTimesheetsDialog open={isUploadDialogOpen} onClose={() => setIsUploadDialogOpen(false)} onSuccess={() => revalidator.revalidate()} />
      )}
    </>
  );
};

interface EmployeeTimesheetsFilterControlsProps {
  availableYears: number[];
  availableMonths: number[];
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
  const singleSelectedMonth = filter.months?.[0];

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
                      : selectedMonthsCount === 1 && singleSelectedMonth !== undefined
                        ? CZECH_MONTH_NAMES[singleSelectedMonth]
                        : `${selectedMonthsCount} měsíců`}
              </span>
              <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-[200px] p-2" align="start">
            <div className="space-y-1">
              <button
                type="button"
                className="flex w-full items-center space-x-2 rounded-sm px-2 py-1.5 text-left hover:bg-accent"
                onClick={() => handleMonthToggle(null)}
              >
                <Checkbox checked={isAllMonthsSelected} className="pointer-events-none" />
                <span className="text-sm font-normal">Všechny měsíce</span>
              </button>
              <div className="border-t my-1" />
              {availableMonths.map((month) => {
                const isSelected = filter.months?.includes(month) ?? false;
                return (
                  <button
                    type="button"
                    key={month}
                    className="flex w-full items-center space-x-2 rounded-sm px-2 py-1.5 text-left hover:bg-accent"
                    onClick={() => handleMonthToggle(month)}
                  >
                    <Checkbox checked={isSelected} className="pointer-events-none" />
                    <span className="text-sm font-normal">{CZECH_MONTH_NAMES[month]}</span>
                  </button>
                );
              })}
            </div>
          </PopoverContent>
        </Popover>
      </div>
      <div className="flex items-center gap-3 pt-6">
        <Checkbox id="only-unapproved" checked={false} disabled />
        <Label htmlFor="only-unapproved" className="text-sm cursor-pointer opacity-50">
          Pouze měsíce s neschválenými výkazy
        </Label>
      </div>
    </>
  );
}

function getAvailableMonthsForYear(availableMonths: number[], _year: number) {
  return availableMonths;
}

function getFallbackYear(availableYears: number[], selectedYear: number) {
  if (availableYears.length === 0 || availableYears.includes(selectedYear)) {
    return null;
  }

  const currentYear = new Date().getFullYear();
  if (availableYears.includes(currentYear)) {
    return currentYear;
  }

  const lastAvailableYear = availableYears[availableYears.length - 1];
  return lastAvailableYear ?? null;
}

// grouping by month is no longer needed (single row per month)
