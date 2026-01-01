import { SearchX } from "lucide-react";
import { Texts } from "@/constants/texts";

export const EmptyState = () => {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-muted-foreground">
      <div className="rounded-full bg-muted p-3">
        <SearchX className="size-5" />
      </div>
      <span className="text-sm font-medium">{Texts.noResults}</span>
    </div>
  );
};
