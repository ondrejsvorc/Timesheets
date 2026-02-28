import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import type { GetContractTimesheetsResponse } from "./api/getContractTimesheets";

export const ContractTimesheets = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetContractTimesheetsResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ContractTimesheetsContent />
      </Await>
    </Suspense>
  );
};

const ContractTimesheetsContent = () => {
  useAsyncValue() as GetContractTimesheetsResponse;

  return (
    <SubPageHeader>
      <SubPageTitle>Výkazy</SubPageTitle>
    </SubPageHeader>
  );
};
