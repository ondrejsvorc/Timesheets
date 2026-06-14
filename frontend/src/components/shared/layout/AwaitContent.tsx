import type { ReactNode } from "react";
import { Suspense } from "react";
import { Await } from "react-router";
import { GenericSkeleton } from "@/components/shared/data/GenericSkeleton";

interface AwaitContentProps {
  promise: Promise<unknown>;
  children: ReactNode;
  fallback?: ReactNode;
}

export const AwaitContent = ({ promise, children, fallback = <GenericSkeleton /> }: AwaitContentProps) => (
  <Suspense fallback={fallback}>
    <Await resolve={promise}>{children}</Await>
  </Suspense>
);
