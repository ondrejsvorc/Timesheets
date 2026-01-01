import { SearchX } from "lucide-react";

export const EmptyState = () => {
  return (
    <div className="flex items-center justify-center gap-2 py-12 text-muted-foreground">
      <SearchX className="size-4" />
      <span>Žádné výsledky</span>
    </div>
  );
};
