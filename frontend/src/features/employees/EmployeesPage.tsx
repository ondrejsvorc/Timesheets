import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { lazy, Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import type { GetEmployeesResponse } from "./api/getEmployees";
import { EmployeesTable } from "./EmployeesTable";
import { type EmployeesFilterCriteria, useEmployeesFilter } from "./hooks/useEmployeesFilters";

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

const { FilterSearchInput } = createFilterControls<EmployeesFilterCriteria>();

const EmployeesPageContent = () => {
  const response = useAsyncValue() as GetEmployeesResponse;
  const { filter, setFilter, filtered } = useEmployeesFilter(response.employees);

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <EmployeesTable employees={filtered} />
    </>
  );
};
