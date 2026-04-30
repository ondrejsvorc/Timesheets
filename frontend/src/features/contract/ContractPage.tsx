import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData, useParams } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { useBackFromLocationState } from "@/hooks/useBackFromLocationState";
import type { GetProjectContractResponse } from "./api/getProjectContract";
import { ContractTabs } from "./ContractTabs";

export const ContractPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractResponse>;
  };

  return (
    <>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <ContractPageHeader />
        </Await>
      </Suspense>
      <Suspense fallback={<GenericSkeleton />}>
        <Outlet />
      </Suspense>
    </>
  );
};

const ContractPageHeader = () => {
  const contract = useAsyncValue() as GetProjectContractResponse;
  const { id: projectId } = useParams<{ id: string }>();
  const handleBack = useBackFromLocationState(() => Routes.project(projectId ?? ""));

  return (
    <>
      <PageHeader leading={<BackButton onClick={handleBack} />}>
        <PageTitle>{contract.name}</PageTitle>
        <PageSubtitle>{contract.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ContractTabs />
    </>
  );
};
