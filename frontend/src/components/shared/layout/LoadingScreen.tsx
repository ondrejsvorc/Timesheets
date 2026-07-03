import { Spinner } from "@/components/ui/spinner";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/common";

type LoadingScreenProps = {
  message?: string;
  className?: string;
};

export const LoadingScreen = ({ message = Texts.loading, className }: LoadingScreenProps) => {
  return (
    <div className={cn("min-h-screen w-full bg-muted text-foreground grid place-items-center animate-in fade-in duration-200", className)}>
      <div className="flex flex-col items-center gap-4">
        <Spinner className="size-12 text-primary [animation-duration:0.6s]" aria-label={message} />
        <div className="text-sm text-muted-foreground">{message}</div>
      </div>
    </div>
  );
};
