// ProjectPage.tsx

import { Suspense, startTransition } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { useImmer, useImmerReducer } from "use-immer";
import { AddButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddProjectDialog } from "./AddProjectDialog";
import type { GetProjectsResponse } from "./api/getProjects";
import { type ProjectsFilterCriteria, useProjectsFilter } from "./hooks/useProjectsFilter";
import { ProjectCards } from "./ProjectCards";
import { ProjectsContext } from "./utils/projectsContext";
import { projectsReducer } from "./utils/projectsReducer";

export const ProjectsPage = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectsResponse>;
  };

  return (
    <>
      <PageHeader>
        <PageTitle>{Texts.projects}</PageTitle>
      </PageHeader>
      <Suspense fallback={<GenericSkeleton />}>
        <Await resolve={promise}>
          <ProjectsPageContent />
        </Await>
      </Suspense>
    </>
  );
};

const { FilterSearchInput, FilterCheckbox } = createFilterControls<ProjectsFilterCriteria>();

const ProjectsPageContent = () => {
  const response = useAsyncValue() as GetProjectsResponse;
  const [state, dispatch] = useImmerReducer(projectsReducer, response.projects);
  const { filter, setFilter, filtered } = useProjectsFilter(state);
  const [isAddOpen, setIsAddOpen] = useImmer(false);

  return (
    <ProjectsContext.Provider value={dispatch}>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={<AddButton onClick={() => startTransition(() => setIsAddOpen(true))}>{Texts.addProject}</AddButton>}
      >
        <FilterSearchInput placeholder={Texts.search} />
        <FilterCheckbox field="onlyActive" label={Texts.activeOnly} />
      </FilterBar>
      <ProjectCards projects={filtered} />
      <AddProjectDialog
        open={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSaved={(project) => {
          dispatch({ type: "add", project });
          setIsAddOpen(false);
        }}
      />
    </ProjectsContext.Provider>
  );
};

// ProjectCards.tsx

import { EmptyState } from "@/components/shared/data/EmptyState";
import type { ProjectItem } from "./api/shared/projectItem";
import { ProjectCard } from "./ProjectCard";

interface ProjectCardsProps {
  projects: ProjectItem[];
}

export const ProjectCards = ({ projects }: ProjectCardsProps) => {
  if (projects.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {projects.map((project) => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
};

// createProject.ts

import { ApiUrl, customFetch, withOptionalDelay } from "@/constants/api";
import type { ProjectItem } from "./shared/projectItem";

export type CreateProjectRequest = {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
};

export type CreateProjectResponse = {
  project: ProjectItem;
};

export const createProject = async (request: CreateProjectRequest, signal: AbortSignal): Promise<CreateProjectResponse> => {
  return withOptionalDelay("fast", () =>
    customFetch<CreateProjectResponse>(`${ApiUrl}/projects`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
      signal,
    }),
  );
};

// EmployeesPage.tsx

import { lazy, Suspense } from "react";
import { Await, useAsyncValue, useLoaderData } from "react-router";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { PageHeader, PageTitle } from "@/components/shared/layout/PageHeader";
import { Texts } from "@/constants/texts";
import { createFilterControls } from "@/utils/createFilterControls";
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

// EmployeesTable.tsx

import { useNavigate } from "react-router";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import type { EmployeeItem } from "./api/getEmployees";

interface EmployeesTableProps {
  employees: EmployeeItem[];
}

export const EmployeesTable = ({ employees }: EmployeesTableProps) => {
  if (employees.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="rounded-md border p-4">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{Texts.personalNumber}</TableHead>
            <TableHead>{Texts.fullName}</TableHead>
            <TableHead>{Texts.email}</TableHead>
            <TableHead>{Texts.employeeType}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {employees.map((employee) => (
            <EmployeeRow key={employee.id} employee={employee} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

interface EmployeeRowProps {
  employee: EmployeeItem;
}

export const EmployeeRow = ({ employee }: EmployeeRowProps) => {
  const navigate = useNavigate();

  return (
    <TableRow className="cursor-pointer" onClick={() => navigate(Routes.employee(employee.id))}>
      <TableCell>{employee.personalNumber ?? Texts.dash}</TableCell>
      <TableCell>{employee.fullName}</TableCell>
      <TableCell>{employee.email ?? Texts.dash}</TableCell>
      <TableCell>{employee.employeeTypeId ? Texts.academic : Texts.nonAcademic}</TableCell>
    </TableRow>
  );
};

// ProjectPage.tsx

import { Suspense } from "react";
import { Await, Outlet, useAsyncValue, useLoaderData, useNavigate } from "react-router";
import { BackButton } from "@/components/shared/buttons/ActionButtons";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";
import { PageHeader, PageSubtitle, PageTitle } from "@/components/shared/layout/PageHeader";
import { Routes } from "@/constants/routes";
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
  const navigate = useNavigate();

  return (
    <>
      <PageHeader leading={<BackButton onClick={() => navigate(Routes.projects())} />}>
        <PageTitle>{response.project.name}</PageTitle>
        <PageSubtitle>{response.project.registrationNumber}</PageSubtitle>
      </PageHeader>
      <ProjectTabs />
    </>
  );
};

// ActionButtons.tsx

import { ArrowLeft, Lock, Maximize2, Minimize2, Pencil, Plus, Save, Trash2, Unlock } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Texts } from "@/constants/texts";
import { BusyButton } from "./BusyButton";

export const ActionButtons = ({ children }: { children: ReactNode }) => <div className="flex items-center gap-2">{children}</div>;

interface AddButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const AddIcon = () => <Plus className="size-4" />;
export const AddButton = ({ onClick, disabled, children }: AddButtonProps) => (
  <Button type="button" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <AddIcon />
      {children}
    </span>
  </Button>
);

interface EditButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const EditIcon = () => <Pencil className="size-4" />;
export const EditButton = ({ onClick, disabled, children }: EditButtonProps) => (
  <Button type="button" variant="outline" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <EditIcon />
      {children}
    </span>
  </Button>
);

interface DeleteButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const DeleteIcon = () => <Trash2 className="size-4" />;
export const DeleteButton = ({ onClick, disabled, children }: DeleteButtonProps) => (
  <Button type="button" variant="destructive" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <Trash2 className="size-4" />
      {children}
    </span>
  </Button>
);

interface SaveButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
  children?: ReactNode;
}

export const SaveIcon = () => <Save className="size-4" />;
export const SaveButton = ({ onClick, disabled, children }: SaveButtonProps) => (
  <BusyButton
    onClick={onClick}
    disabled={disabled}
    icon={<Save className="size-4" />}
    type="submit"
    onSuccess={() => toast.success(Texts.actionSuccessful)}
    onError={() => toast.error(Texts.actionFailed)}
  >
    {children ?? null}
  </BusyButton>
);

interface FullscreenButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
  disabled?: boolean;
  isFullscreen?: boolean;
  children?: ReactNode;
}

export const FullscreenIcon = ({ isFullscreen = false }: { isFullscreen?: boolean }) =>
  isFullscreen ? <Minimize2 className="size-4" /> : <Maximize2 className="size-4" />;
export const FullscreenButton = ({ onClick, disabled, isFullscreen = false, children }: FullscreenButtonProps) => (
  <Button type="button" variant="outline" onClick={onClick} disabled={disabled}>
    <span className="inline-flex items-center gap-2">
      <FullscreenIcon isFullscreen={isFullscreen} />
      {children ?? (isFullscreen ? Texts.exitFullscreen : Texts.enterFullscreen)}
    </span>
  </Button>
);

interface BackButtonProps {
  onClick: () => void;
}

export const BackIcon = () => <ArrowLeft className="size-4" />;
export const LockIcon = () => <Lock className="size-4" />;
export const UnlockIcon = () => <Unlock className="size-4" />;

// BusyButton.tsx

import { Loader2 } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";

interface BusyButtonProps {
  onClick: (event: MouseEvent<HTMLButtonElement>, signal: AbortSignal) => Promise<void>;
  disabled?: boolean;
  icon: ReactNode;
  children: ReactNode;
  type?: "button" | "submit";
  onSuccess?: () => void;
  onError?: (error: unknown) => void;
}

export const BusyButton = ({ onClick, disabled = false, icon, children, type = "button", onSuccess, onError }: BusyButtonProps) => {
  const [isBusy, setIsBusy] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const handleClick = async (event: MouseEvent<HTMLButtonElement>) => {
    if (isBusy) {
      return;
    }

    const controller = new AbortController();
    abortRef.current = controller;

    setIsBusy(true);
    try {
      await onClick(event, controller.signal);
      if (!controller.signal.aborted) {
        onSuccess?.();
      }
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }
      onError?.(error);
    } finally {
      setIsBusy(false);
      abortRef.current = null;
    }
  };

  return (
    <Button type={type} onClick={handleClick} disabled={disabled || isBusy}>
      <span className="inline-flex items-center gap-2">
        {isBusy ? <Loader2 className="size-4 animate-spin opacity-60 [animation-duration:0.5s]" /> : icon}
        {children}
      </span>
    </Button>
  );
};

// PageHeader.tsx

interface PageHeaderProps {
    leading?: React.ReactNode;
    actions?: React.ReactNode;
    children: React.ReactNode;
  }
  
  export const PageHeader = ({ leading, actions, children }: PageHeaderProps) => {
    return (
      <div className="flex items-start justify-between gap-4 mb-6">
        <div className="flex items-start gap-3 min-w-0">
          {leading && <div className="shrink-0 mt-1">{leading}</div>}
          <div className="min-w-0 space-y-1">{children}</div>
        </div>
        {actions && <div className="shrink-0 flex items-center gap-2">{actions}</div>}
      </div>
    );
  };
  
  export const PageTitle = ({ children }: { children: React.ReactNode }) => {
    return <h1 className="text-3xl font-semibold leading-tight tracking-tight select-none text-foreground">{children}</h1>;
  };
  
  export const PageSubtitle = ({ children }: { children: React.ReactNode }) => {
    return <p className="text-sm text-muted-foreground leading-relaxed">{children}</p>;
  };

// HumanTooltip.tsx

import type { ReactNode } from "react";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

interface HoursToHumanTooltipProps {
  hours: number;
  children: ReactNode;
}

const formatHoursToHuman = (hours: number): string => {
  const sign = hours < 0 ? "-" : "";
  const totalMinutes = Math.round(Math.abs(hours) * 60);
  const wholeHours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return `${sign}${wholeHours}h ${minutes}m`;
};

export const HoursToHumanTooltip = ({ hours, children }: HoursToHumanTooltipProps) => {
  return (
    <Tooltip delayDuration={100}>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent side="top">
        <p className="font-medium text-xs">{formatHoursToHuman(hours)}</p>
      </TooltipContent>
    </Tooltip>
  );
};