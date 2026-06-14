import { denyUnless } from "@/auth/routeGuards";
import { UiAction } from "@/auth/uiPermissions";
import { getContractEmployeeUpdateImpact } from "@/features/contract/api/contractEmployeeUpdateImpact";
import type { UpdateContractEmployeeRequest } from "@/features/contract/api/updateContractEmployee";
import { getContractCatalog } from "@/features/employees/api/getContractCatalog";
import { getEmployees } from "@/features/employees/api/getEmployees";
import { getProjectCatalog } from "@/features/employees/api/getProjectCatalog";
import { getContractDeleteImpact } from "@/features/project/api/contractDeleteImpact";
import { getProjectContracts } from "@/features/project/api/getProjectContracts";
import { getProjectDeleteImpact } from "@/features/projects/api/projectDeleteImpact";

export const resourceRoutes = [
  {
    path: "_resources/projects",
    loader: async () => getProjectCatalog(),
  },
  {
    path: "_resources/contracts",
    loader: async ({ request }: { request: Request }) => {
      const projectId = new URL(request.url).searchParams.get("projectId");
      if (!projectId) {
        throw new Response("projectId is required", { status: 400 });
      }
      return getContractCatalog(projectId);
    },
  },
  {
    path: "_resources/employees",
    loader: async ({ request }: { request: Request }) => {
      await denyUnless(UiAction.employees.list, {}, request);
      return getEmployees().promise;
    },
  },
  {
    path: "_resources/project-contracts/:projectId",
    loader: async ({ params }: { params: { projectId?: string } }) => {
      if (!params.projectId) {
        throw new Response("projectId is required", { status: 400 });
      }
      const [contracts, employees] = await Promise.all([getProjectContracts(params.projectId).promise, getEmployees().promise]);
      return { contracts: contracts.projectContracts, employees: employees.employees };
    },
  },
  {
    path: "_resources/project-delete-impact/:projectId",
    loader: async ({ params, request }: { params: { projectId?: string }; request: Request }) => {
      if (!params.projectId) {
        throw new Response("projectId is required", { status: 400 });
      }
      await denyUnless(UiAction.projects.delete, { projectId: params.projectId }, request);
      return getProjectDeleteImpact(params.projectId);
    },
  },
  {
    path: "_resources/contract-delete-impact/:contractId",
    loader: async ({ params, request }: { params: { contractId?: string }; request: Request }) => {
      if (!params.contractId) {
        throw new Response("contractId is required", { status: 400 });
      }
      await denyUnless(UiAction.contracts.delete, { contractId: params.contractId }, request);
      return getContractDeleteImpact(params.contractId);
    },
  },
  {
    path: "_resources/contract-employee-update-impact/:contractId/:contractEmployeeId",
    action: async ({ params, request }: { params: { contractId?: string; contractEmployeeId?: string }; request: Request }) => {
      if (!params.contractId || !params.contractEmployeeId) {
        throw new Response("contractId and contractEmployeeId are required", { status: 400 });
      }
      await denyUnless(UiAction.contractEmployees.update, { contractId: params.contractId }, request);
      const body = (await request.json()) as UpdateContractEmployeeRequest;
      return getContractEmployeeUpdateImpact(params.contractId, params.contractEmployeeId, body);
    },
  },
] as const;
