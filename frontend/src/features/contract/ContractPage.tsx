import { Suspense } from "react";
import { useAsyncValue, useLoaderData, useParams } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { TabbedOutlet } from "@/components/shared/layout/TabbedOutlet";
import { Routes } from "@/constants/routes";
import { useGo } from "@/hooks/useGo";
import type { GetProjectContractResponse } from "./api";
import { ContractTabs } from "./ContractTabs";

export const ContractPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractResponse>;
  };

  return (
    <>
      <AwaitContent promise={promise}>
        <ContractPageHeader />
      </AwaitContent>
      <Suspense fallback={<GenericSkeleton />}>
        <TabbedOutlet />
      </Suspense>
    </>
  );
};

const ContractPageHeader = () => {
  const contract = useAsyncValue() as GetProjectContractResponse;
  const { id: projectId } = useParams<{ id: string }>();
  const go = useGo();

  return (
    <>
      <PageHeader leading={<BackButton onClick={go.back(() => Routes.project(projectId ?? ""))} />}>
        <PageTitle>{contract.name}</PageTitle>
        <PageSubtitle>{contract.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ContractTabs />
    </>
  );
};
