import { denyUnless } from "@/auth/routeGuards";
import { UiAction } from "@/auth/uiPermissions";
import { getContractCatalog } from "@/features/employees/api/getContractCatalog";
import { getEmployees } from "@/features/employees/api/getEmployees";
import { getProjectCatalog } from "@/features/employees/api/getProjectCatalog";
import { getProjectContracts } from "@/features/project/api/getProjectContracts";

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
    loader: async () => {
      await denyUnless(UiAction.employees.list);
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
] as const;
