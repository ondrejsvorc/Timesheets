export const Routes = {
  projects: () => "/projects",
  project: (id: string) => `/projects/${id}`,
  projectContracts: (id: string) => `/projects/${id}`,
  projectContractsManagers: (id: string) => `/projects/${id}/contracts-managers`,
  contract: (projectId: string, contractId: string) => `/projects/${projectId}/contracts/${contractId}`,
  contractTimesheets: (projectId: string, contractId: string) => `/projects/${projectId}/contracts/${contractId}`,
  contractEmployees: (projectId: string, contractId: string) => `/projects/${projectId}/contracts/${contractId}/employees`,
  employees: () => "/employees",
  employee: (id: string) => `/employees/${id}`,
  employeeTimesheets: (id: string) => `/employees/${id}/timesheets`,
  timesheet: (employeeId: string, year: number, month: number) => `/timesheet?employeeId=${employeeId}&year=${year}&month=${month}`,
};
