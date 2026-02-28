import { BackButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData, useNavigate, useParams } from "react-router";
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
  const navigate = useNavigate();
  const projectId = useParams().id;

  return (
    <>
      <PageHeader
        leading={<BackButton onClick={() => navigate(Routes.project(projectId ?? ""))} />}
        actions={<EditButton onClick={() => {}}>{Texts.editContract}</EditButton>}
      >
        <PageTitle>{contract.name}</PageTitle>
        <PageSubtitle>{contract.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ContractTabs />
    </>
  );
};
