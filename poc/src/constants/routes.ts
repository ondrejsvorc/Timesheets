export const Routes = {
  projects: () => "/projects",
  project: (id: string) => `/projects/${id}`,
  projectContracts: (id: string) => `/projects/${id}`,
  projectContractsManagers: (id: string) => `/projects/${id}/contracts-managers`,
  employees: () => "/employees",
  employee: (id: string) => `/employees/${id}`,
  employeeTimesheets: (id: string) => `/employees/${id}/timesheets`,
};
