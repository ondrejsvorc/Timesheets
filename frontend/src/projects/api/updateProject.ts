import { Constants } from "../../common/Constants";

export type UpdateProjectRequest = {
  name: string;
  registrationNumber: string;
  recipientName: string;
  startDate: string;
  endDate: string | null;
  description: string;
};

export const updateProject = async (id: string, request: UpdateProjectRequest): Promise<void> => {
  const response = await fetch(`${Constants.apiUrl}/projects/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error("Failed to update project.");
};

