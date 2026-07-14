import { Suspense } from "react";
import { useAsyncValue, useLoaderData } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { TabbedOutlet } from "@/components/shared/layout/TabbedOutlet";
import { Routes } from "@/constants/routes";
import { useGo } from "@/hooks/useGo";
import type { GetEmployeeResponse } from "./api";
import { EmployeeTabs } from "./EmployeeTabs";
import { resolveEmployeeTypeName } from "./employeeType";

export const EmployeePage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetEmployeeResponse>;
  };

  return (
    <>
      <AwaitContent promise={promise}>
        <EmployeePageHeader />
      </AwaitContent>
      <Suspense fallback={<GenericSkeleton />}>
        <TabbedOutlet />
      </Suspense>
    </>
  );
};

const EmployeePageHeader = () => {
  const response = useAsyncValue() as GetEmployeeResponse;
  const employee = response.employee;
  const go = useGo();
  const employeeType = resolveEmployeeTypeName(employee.employeeTypeId);
  const subtitleParts = [employee.personalNumber, employeeType].filter((v) => Boolean(v && v.trim().length > 0));

  return (
    <>
      <PageHeader leading={<BackButton onClick={go.back(Routes.employees())} />}>
        <PageTitle>{employee.fullName}</PageTitle>
        <PageSubtitle>{subtitleParts.join(" · ")}</PageSubtitle>
      </PageHeader>
      <EmployeeTabs />
    </>
  );
};
