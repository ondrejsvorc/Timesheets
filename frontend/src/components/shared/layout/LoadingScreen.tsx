import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";

type LoadingScreenProps = {
  message?: string;
  className?: string;
};

export const LoadingScreen = ({ message = Texts.loading, className }: LoadingScreenProps) => {
  return (
    <div className={cn("relative min-h-screen w-full overflow-hidden bg-background", className)}>
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_80%_60%_at_50%_40%,color-mix(in_oklab,var(--primary)_12%,transparent),transparent)]" />
      <div className="relative grid min-h-screen place-items-center px-6">
        <div className="flex flex-col items-center gap-5 animate-in fade-in duration-300">
          <div className="relative size-14" aria-hidden>
            <div className="absolute inset-0 rounded-full bg-primary/10 animate-ping [animation-duration:2.4s]" />
            <div className="absolute inset-0 rounded-full border-[3px] border-primary/15" />
            <div className="absolute inset-0 rounded-full border-[3px] border-primary border-t-transparent animate-spin [animation-duration:0.85s]" />
          </div>
          <p className="text-sm font-medium tracking-wide text-muted-foreground" aria-live="polite">
            {message}
          </p>
        </div>
      </div>
    </div>
  );
};
