export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string | null;
  startDate: string;
  endDate?: string | null;
  contractCount: number;
}
