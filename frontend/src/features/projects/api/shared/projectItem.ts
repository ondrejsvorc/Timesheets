export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate?: string | null;
  contractCount: number;
}
