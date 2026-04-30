export const AppFooter = () => (
  <footer className="w-full bg-background border-t border-border/50 mt-auto">
    <div className="mx-auto max-w-7xl h-16 flex items-center justify-center px-6 text-sm text-muted-foreground select-none">
      © UJEP {new Date().getFullYear()} • Poslední nasazení: {new Date().toLocaleString('cs-CZ', { day: '2-digit', month: '2-digit', year: 'numeric', hour: 'numeric', minute: '2-digit' })}
    </div>
  </footer>
);
