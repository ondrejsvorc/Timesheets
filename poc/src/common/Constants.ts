export const Constants = {
  apiUrl: import.meta.env.DEV ? "https://localhost:7096/api" : "insert production url here",
} as const;
