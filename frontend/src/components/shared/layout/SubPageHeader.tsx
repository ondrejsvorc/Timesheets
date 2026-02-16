interface SubPageHeaderProps {
  children: React.ReactNode;
}

export const SubPageHeader = ({ children }: SubPageHeaderProps) => {
  return (
    <div className="py-6">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3 min-w-0">
          <div className="min-w-0 space-y-1">{children}</div>
        </div>
      </div>
    </div>
  );
};

export const SubPageTitle = ({ children }: { children: React.ReactNode }) => {
  return <h2 className="text-xl font-semibold leading-tight tracking-tight select-none text-foreground">{children}</h2>;
};

export const SubPageSubtitle = ({ children }: { children: React.ReactNode }) => {
  return <p className="text-sm text-muted-foreground leading-relaxed">{children}</p>;
};
