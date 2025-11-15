const isDev = import.meta.env.DEV;

export const Constants = {
  apiUrl: isDev ? "https://localhost:7096/api" : "insert production url here"
} as const;
