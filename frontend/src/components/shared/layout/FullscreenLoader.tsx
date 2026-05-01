import { Spinner } from "@/components/ui/spinner";
import { Texts } from "@/constants/texts";

type FullscreenLoaderProps = {
  ariaLabel?: string;
};

export const FullscreenLoader = ({ ariaLabel = Texts.loading }: FullscreenLoaderProps) => {
  return (
    <div className="min-h-screen w-full bg-muted text-foreground grid place-items-center animate-in fade-in duration-200">
      <div className="flex flex-col items-center gap-4">
        <Spinner className="size-12 text-primary [animation-duration:0.6s]" aria-label={ariaLabel} />
      </div>
    </div>
  );
};
