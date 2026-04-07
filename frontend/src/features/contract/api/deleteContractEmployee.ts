import { ApiUrl, customFetch } from "@/constants/api";

export const deleteContractEmployee = async (contractId: string, contractEmployeeId: string, signal?: AbortSignal) => {
  return await customFetch<void>(`${ApiUrl}/contracts/${contractId}/employees/${contractEmployeeId}`, {
    method: "DELETE",
    signal,
  });
};
