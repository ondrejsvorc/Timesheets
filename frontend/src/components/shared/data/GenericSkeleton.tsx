import { Skeleton } from "@/components/ui/skeleton";

export const GenericSkeleton = () => (
  <div className="w-full space-y-3 py-3">
    <Skeleton className="h-5 w-2/5 [animation-duration:0.5s]" />
    <Skeleton className="h-4 w-full [animation-duration:0.5s]" />
    <Skeleton className="h-4 w-5/6 [animation-duration:0.5s]" />
    <Skeleton className="h-4 w-3/4 [animation-duration:0.5s]" />
  </div>
);
