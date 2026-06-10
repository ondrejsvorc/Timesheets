import type { ShouldRevalidateFunction } from "react-router";

const isResourceRequest = (url: string) => url.includes("/_resources/");

/** Zabrání zbytečnému přenačítání stránkových loaderů při fetcher.load na _resources routy. */
export const skipRevalidateForResourceFetchers: ShouldRevalidateFunction = ({ formAction, nextUrl, defaultShouldRevalidate }) => {
  if (formAction && isResourceRequest(formAction)) {
    return false;
  }

  if (isResourceRequest(nextUrl.pathname)) {
    return false;
  }

  return defaultShouldRevalidate;
};
