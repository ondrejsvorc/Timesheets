import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { TabbedOutlet } from "@/components/shared/layout/TabbedOutlet";
import { Routes } from "@/constants/routes";
import { useBackFromLocationState } from "@/hooks/useBackFromLocationState";
import { resolveEmployeeTypeName } from "@/utils/resolveEmployeeTypeName";
import type { GetEmployeeResponse } from "./api/getEmployee";
import { EmployeeTabs } from "./EmployeeTabs";

export const EmployeePage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeeResponse>;
  };

  return (
    <>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <EmployeePageHeader />
        </Await>
      </Suspense>
      <Suspense fallback={<GenericSkeleton />}>
        <TabbedOutlet />
      </Suspense>
    </>
  );
};

const EmployeePageHeader = () => {
  const response = useAsyncValue() as GetEmployeeResponse;
  const employee = response.employee;
  const handleBack = useBackFromLocationState(Routes.employees());
  const employeeType = resolveEmployeeTypeName(employee.employeeTypeId);
  const subtitleParts = [employee.personalNumber, employee.email, employeeType].filter((v) => Boolean(v && v.trim().length > 0));

  return (
    <>
      <PageHeader leading={<BackButton onClick={handleBack} />}>
        <PageTitle>{employee.fullName}</PageTitle>
        <PageSubtitle>{subtitleParts.join(" · ")}</PageSubtitle>
      </PageHeader>
      <EmployeeTabs />
    </>
  );
};
