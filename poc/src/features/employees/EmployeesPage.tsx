import { lazy, Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import type { GetEmployeesResponse } from "./api/getEmployees";
import { EmployeesFilter } from "./EmployeesFilter";
import { EmployeesTable } from "./EmployeesTable";
import { useEmployeeFilters } from "./hooks/useEmployeeFilters";

const EmployeesPageContentLazy = lazy(async () => ({
  default: EmployeesPageContent,
}));

export const EmployeesPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeesResponse>;
  };

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.employees}</PageTitle>
      </PageHeader>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <EmployeesPageContentLazy />
        </Await>
      </Suspense>
    </>
  );
};

const EmployeesPageContent = () => {
  const response = useAsyncValue() as GetEmployeesResponse;
  const { filters, setFilters, filtered } = useEmployeeFilters(response.employees);

  return (
    <>
      <FilterBar>
        <EmployeesFilter value={filters} onChange={setFilters} />
      </FilterBar>
      <EmployeesTable employees={filtered} />
    </>
  );
};
