import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import {
  confirmTimesheetImport,
  type FileSelection,
} from "./api/uploadTimesheets";
import { useParams, useNavigate } from "react-router";
import type { EmployeePositionItem } from "./api/getEmployeePositions";

interface UploadTimesheetsDialogProps {
  open: boolean;
  files: File[];
  onClose: () => void;
  onSuccess?: () => void;
  positionsPromise?: Promise<{ employeeId: string; positions: EmployeePositionItem[] }>;
}

export const UploadTimesheetsDialog = ({
  open,
  files,
  onClose,
  onSuccess,
  positionsPromise,
}: UploadTimesheetsDialogProps) => {
  const { id: employeeIdFromUrl } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [positions, setPositions] = useState<EmployeePositionItem[]>([]);
  const [isImporting, setIsImporting] = useState(false);

  useEffect(() => {
    let cancelled = false;

    if (positionsPromise) {
      positionsPromise.then((data) => {
        if (!cancelled) {
          setPositions(data.positions);
        }
      });
    }

    return () => {
      cancelled = true;
    };
  }, [positionsPromise]);

  const handleImport = async () => {
    if (!employeeIdFromUrl) {
      toast.error("Není vybrán zaměstnanec");
      return;
    }

    if (files.length === 0) {
      toast.error("Žádné soubory k importu");
      return;
    }

    if (positions.length === 0) {
      toast.error("Zaměstnanec nemá žádné kontrakty pro přiřazení výkazů");
      return;
    }

    setIsImporting(true);

    try {
      const currentYear = new Date().getFullYear();

      const selections: FileSelection[] = files.map((file, index) => {
        // Jednoduchá heuristika: aktuální rok, měsíc = index+1 (nebo 1 pokud přesahuje)
        let month = (index % 12) + 1;
        const year = currentYear;

        // Zkusit najít aktivní kontrakt pro dané období, jinak první kontrakt
        const periodDate = new Date(year, month - 1, 1);
        const activeContract =
          positions.find(
            (p) =>
              new Date(p.startDate) <= periodDate &&
              (p.endDate === null || new Date(p.endDate) >= periodDate),
          ) ?? positions[0]!;

        return {
          fileName: file.name,
          employeeId: employeeIdFromUrl,
          contractId: activeContract.contractId,
          year,
          month,
        };
      });

      const response = await confirmTimesheetImport(files, selections);
      const successCount = response.results.filter((r) => r.success).length;
      const failCount = response.results.length - successCount;

      if (successCount > 0) {
        toast.success(`Úspěšně importováno ${successCount} souborů`);
      }
      if (failCount > 0) {
        toast.error(`Nepodařilo se importovat ${failCount} souborů`);
      }

      if (successCount > 0 && failCount === 0) {
        onSuccess?.();
        onClose();
        navigate(0);
      }
    } catch (error) {
      toast.error("Chyba při importu souborů");
      // eslint-disable-next-line no-console
      console.error(error);
    } finally {
      setIsImporting(false);
    }
  };

  const handleClose = () => {
    if (isImporting) {
      return;
    }
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Nahrát výkazy</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="border rounded-md p-3 max-h-[400px] overflow-y-auto">
            {files.length === 0 ? (
              <div className="text-sm text-muted-foreground">Zatím nejsou vybrány žádné soubory.</div>
            ) : (
              <ul className="space-y-1 text-sm">
                {files.map((file) => (
                  <li key={file.name} className="flex items-center justify-between gap-2">
                    <span className="truncate">{file.name}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={isImporting}>
            Zrušit
          </Button>
          <Button onClick={handleImport} disabled={files.length === 0 || isImporting}>
            {isImporting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Importuji...
              </>
            ) : (
              "Importovat"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};