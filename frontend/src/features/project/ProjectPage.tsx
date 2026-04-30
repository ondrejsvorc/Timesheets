import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
import { useBackFromLocationState } from "@/hooks/useBackFromLocationState";
import type { GetProjectResponse } from "./api/getProject";
import { ProjectTabs } from "./ProjectTabs";

export const ProjectPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectResponse>;
  };

  return (
    <>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <ProjectPageHeader />
        </Await>
      </Suspense>
      <Suspense fallback={<GenericSkeleton />}>
        <Outlet />
      </Suspense>
    </>
  );
};

const ProjectPageHeader = () => {
  const response = useAsyncValue() as GetProjectResponse;
  const handleBack = useBackFromLocationState(Routes.projects());

  return (
    <>
      <PageHeader leading={<BackButton onClick={handleBack} />}>
        <PageTitle>{response.project.name}</PageTitle>
        <PageSubtitle>{response.project.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ProjectTabs />
    </>
  );
};
