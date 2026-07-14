import { Suspense } from "react";
import { useAsyncValue, useLoaderData } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { TabbedOutlet } from "@/components/shared/layout/TabbedOutlet";
import { Routes } from "@/constants/routes";
import { useGo } from "@/hooks/useGo";
import type { GetProjectResponse } from "./api";
import { ProjectTabs } from "./ProjectTabs";

export const ProjectPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectResponse>;
  };

  return (
    <>
      <AwaitContent promise={promise}>
        <ProjectPageHeader />
      </AwaitContent>
      <Suspense fallback={<GenericSkeleton />}>
        <TabbedOutlet />
      </Suspense>
    </>
  );
};

const ProjectPageHeader = () => {
  const response = useAsyncValue() as GetProjectResponse;
  const go = useGo();

  return (
    <>
      <PageHeader leading={<BackButton onClick={go.back(Routes.projects())} />}>
        <PageTitle>{response.project.name}</PageTitle>
        <PageSubtitle>{response.project.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ProjectTabs />
    </>
  );
};
