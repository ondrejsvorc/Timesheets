import { Constants } from "../../common/Constants";

export const deleteProject = async (id: string): Promise<void> => {
  const response = await fetch(`${Constants.apiUrl}/projects/${id}`, {
    method: "DELETE",
  });
  if (!response.ok) throw new Error("Failed to delete project.");
};

