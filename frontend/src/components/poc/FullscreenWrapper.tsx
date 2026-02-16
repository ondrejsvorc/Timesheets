import { Maximize2, Minimize2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/utils/cn";

interface FullscreenWrapperProps {
  children: React.ReactNode;
}

export const FullscreenWrapper = ({ children }: FullscreenWrapperProps) => {
  const [isFullscreen, setIsFullscreen] = useState(false);

  return (
    <div
      className={cn(
        "bg-background transition-all duration-300",
        isFullscreen && "fixed inset-0 z-[100] p-4 flex flex-col h-screen w-screen overflow-hidden",
      )}
    >
      <div className="flex justify-between items-center mb-4">
        <Button variant="outline" size="sm" onClick={() => setIsFullscreen(!isFullscreen)} className="ml-auto flex gap-2">
          {isFullscreen ? (
            <>
              <Minimize2 className="size-4" /> Zpět
            </>
          ) : (
            <>
              <Maximize2 className="size-4" /> Režim celé obrazovky
            </>
          )}
        </Button>
      </div>

      {/* Tento div zajistí, že tabulka ve fullscreenu využije 100% zbývající výšky */}
      <div className={cn("flex-1 overflow-hidden", isFullscreen && "h-full")}>{children}</div>
    </div>
  );
};
