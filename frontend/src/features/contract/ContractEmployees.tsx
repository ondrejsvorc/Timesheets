import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Texts } from "@/constants/texts";
import { Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import type { GetContractEmployeesResponse } from "./api/getContractEmployees";

export const ContractEmployees = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetContractEmployeesResponse>;
  };

  return (
    <Suspense fallback={<GenericSkeleton />}>
      <Await resolve={promise}>
        <ContractEmployeesContent />
      </Await>
    </Suspense>
  );
};

const ContractEmployeesContent = () => {
  useAsyncValue() as GetContractEmployeesResponse;

  return (
    <SubPageHeader>
      <SubPageTitle>{Texts.employees}</SubPageTitle>
    </SubPageHeader>
  );
};
