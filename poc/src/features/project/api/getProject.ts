export interface ProjectItem {
  id: string;
  name: string;
  registrationNumber: string;
}

export interface GetProjectResponse {
  project: ProjectItem;
}

const mockResponse: GetProjectResponse = {
  project: {
    id: "4efc77cd-0479-4b57-b2c2-11783faf4063",
    name: "Digitalizace vzdělávacích procesů",
    registrationNumber: "CZ.02.3.68/0.0/0.0/19_076/001",
  },
};

export const getProject = (id: string) => {
  return {
    promise: (async (): Promise<GetProjectResponse> => {
      await new Promise((resolve) => setTimeout(resolve, 600));
      return mockResponse;
    })(),
  };
};
