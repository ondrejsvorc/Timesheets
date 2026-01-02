export const Routes = {
  projects: () => "/projects",
  employees: () => "/employees",
  project: (id: string) => `/projects/${id}`,
  projectContracts: (id: string) => `/projects/${id}`,
  projectContractsManagers: (id: string) => `/projects/${id}/contracts-managers`,
};
