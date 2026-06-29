import { CheckCircle2, CircleDashed, FileText, Loader2, Upload, XCircle } from "lucide-react";
import { useRef, useState } from "react";
import { useParams } from "react-router";
import { toast } from "sonner";
import { DeleteButton } from "@/components/shared/buttons/ActionButtons";
import { DialogCancelButton, DialogConfirmButton } from "@/components/shared/buttons/DialogButtons";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Texts } from "@/constants/texts";
import { cn } from "@/utils/cn";
import { detectTimesheetImport, type ImportResult, importTimesheet, type TimesheetDetectionResult } from "./api";

interface UploadTimesheetsDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

type UploadDialogMode = "selection" | "result";
type UploadItemStatus = "detecting" | "ready" | "invalid" | "importing" | "success" | "error";

interface UploadItem {
  key: string;
  file: File;
  status: UploadItemStatus;
  detection: TimesheetDetectionResult | null;
  result: ImportResult | null;
}

const formatCountText = (template: string, count: number) => template.replace("{count}", String(count));
const getFileKey = (file: File) => `${file.name}-${file.size}-${file.lastModified}`;
const formatPeriod = (year: number | null, month: number | null) => (year && month ? `${String(month).padStart(2, "0")}/${year}` : null);
const getResultDescription = (successCount: number, failCount: number) => {
  if (successCount > 0 && failCount > 0) {
    return `${formatCountText(Texts.importSuccessCount, successCount)} ${formatCountText(Texts.importFailureCount, failCount)}`;
  }

  if (successCount > 0) {
    return formatCountText(Texts.importSuccessCount, successCount);
  }

  return formatCountText(Texts.importFailureCount, failCount);
};

export const UploadTimesheetsDialog = ({ open, onClose, onSuccess }: UploadTimesheetsDialogProps) => {
  const { id: employeeIdFromUrl } = useParams<{ id: string }>();
  const [mode, setMode] = useState<UploadDialogMode>("selection");
  const [isImporting, setIsImporting] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [hasSuccessfulImport, setHasSuccessfulImport] = useState(false);
  const [uploadItems, setUploadItems] = useState<UploadItem[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const addFiles = (newFiles: File[]) => {
    const validFiles = newFiles.filter((file) => /\.(xls|xlsx)$/i.test(file.name));

    if (validFiles.length !== newFiles.length) {
      toast.error(Texts.importXlsOnly);
    }

    if (validFiles.length === 0) {
      return;
    }

    const queuedKeys = new Set(uploadItems.map((item) => item.key));
    const filesToDetect = validFiles.filter((file) => {
      const key = getFileKey(file);
      if (queuedKeys.has(key)) {
        return false;
      }

      queuedKeys.add(key);
      return true;
    });

    if (filesToDetect.length === 0) {
      return;
    }

    setUploadItems((current) => {
      const currentKeys = new Set(current.map((item) => item.key));
      const added = filesToDetect
        .filter((file) => !currentKeys.has(getFileKey(file)))
        .map((file) => ({ key: getFileKey(file), file, status: "detecting" as const, detection: null, result: null }));

      return [...current, ...added];
    });

    setMode("selection");
    void detectFiles(filesToDetect);
  };

  const handleFileInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    addFiles(Array.from(event.target.files ?? []));
    event.target.value = "";
  };

  const handleDrop = (event: React.DragEvent) => {
    event.preventDefault();
    setIsDragging(false);
    addFiles(Array.from(event.dataTransfer.files));
  };

  const handleRemoveFile = (fileToRemove: File) => {
    setUploadItems((current) => current.filter((item) => item.key !== getFileKey(fileToRemove)));
  };

  const detectFiles = async (files: File[]) => {
    if (!employeeIdFromUrl) {
      return;
    }

    for (const file of files) {
      const key = getFileKey(file);

      try {
        const detection = await detectTimesheetImport(employeeIdFromUrl, file);
        setUploadItems((current) =>
          current.map((item) =>
            item.key === key
              ? {
                  ...item,
                  status: detection.canImport ? "ready" : "invalid",
                  detection,
                }
              : item,
          ),
        );
      } catch (error) {
        const detection: TimesheetDetectionResult = {
          fileName: file.name,
          canImport: false,
          isReimport: false,
          errorMessage: Texts.importError,
          employeePersonalNumber: null,
          employeeName: null,
          year: null,
          month: null,
        };

        setUploadItems((current) => current.map((item) => (item.key === key ? { ...item, status: "invalid", detection } : item)));
        // eslint-disable-next-line no-console
        console.error(error);
      }
    }
  };

  const handleImport = async (signal: AbortSignal) => {
    if (!employeeIdFromUrl) {
      toast.error(Texts.noEmployeeSelected);
      return;
    }

    if (uploadItems.length === 0) {
      toast.error(Texts.noFilesToImport);
      return;
    }

    const itemsToImport = uploadItems.filter((item) => item.status === "ready");

    if (itemsToImport.length === 0) {
      toast.error(Texts.importError);
      return;
    }

    setIsImporting(true);
    setHasSuccessfulImport(false);
    setUploadItems((current) =>
      current.map((item) =>
        item.status === "ready" || item.status === "success" || item.status === "error" ? { ...item, status: item.detection?.canImport ? "ready" : "invalid", result: null } : item,
      ),
    );

    try {
      let successCount = 0;
      let failCount = 0;

      for (const item of itemsToImport) {
        if (signal.aborted) {
          break;
        }

        setUploadItems((current) => current.map((currentItem) => (currentItem.key === item.key ? { ...currentItem, status: "importing" } : currentItem)));

        try {
          const result = await importTimesheet(employeeIdFromUrl, item.file, signal);
          const status: UploadItemStatus = result.success ? "success" : "error";
          successCount += result.success ? 1 : 0;
          failCount += result.success ? 0 : 1;

          setUploadItems((current) => current.map((currentItem) => (currentItem.key === item.key ? { ...currentItem, status, result } : currentItem)));
        } catch (error) {
          if (error instanceof DOMException && error.name === "AbortError") {
            throw error;
          }

          failCount += 1;
          const result: ImportResult = {
            fileName: item.file.name,
            success: false,
            errorMessage: Texts.importError,
            timesheetId: null,
            year: null,
            month: null,
          };

          setUploadItems((current) => current.map((currentItem) => (currentItem.key === item.key ? { ...currentItem, status: "error", result } : currentItem)));
          // eslint-disable-next-line no-console
          console.error(error);
        }
      }

      if (successCount > 0) {
        setHasSuccessfulImport(true);
        onSuccess?.();
      }

      if (successCount > 0 && failCount === 0 && !signal.aborted) {
        // Close dialog immediately on full success (no need to show result screen).
        setMode("selection");
        setUploadItems([]);
        setIsDragging(false);
        setHasSuccessfulImport(false);
        onClose();
        return;
      }

      setMode("result");
    } catch (error) {
      if (!(error instanceof DOMException && error.name === "AbortError")) {
        toast.error(Texts.importError);
        // eslint-disable-next-line no-console
        console.error(error);
      }
    } finally {
      setIsImporting(false);
    }
  };

  const handleClose = () => {
    if (isImporting) {
      return;
    }
    const shouldRefresh = hasSuccessfulImport;
    setMode("selection");
    setUploadItems([]);
    setIsDragging(false);
    setHasSuccessfulImport(false);
    onClose();
    if (shouldRefresh) onSuccess?.();
  };

  const readyItemsCount = uploadItems.filter((item) => item.status === "ready").length;
  const successCount = uploadItems.filter((item) => item.status === "success").length;
  const failCount = uploadItems.filter((item) => item.status === "error" || item.status === "invalid").length;
  const isDetecting = uploadItems.some((item) => item.status === "detecting");

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{mode === "selection" ? Texts.import : Texts.importResultTitle}</DialogTitle>
          <DialogDescription>{mode === "selection" ? Texts.importAttendanceOnly : getResultDescription(successCount, failCount)}</DialogDescription>
        </DialogHeader>

        {mode === "selection" ? (
          <div className="space-y-4">
            <input ref={fileInputRef} type="file" multiple accept=".xls,.xlsx" onChange={handleFileInputChange} className="hidden" />
            <button
              type="button"
              className={cn(
                "flex min-h-64 w-full cursor-pointer flex-col items-center justify-center rounded-md border border-dashed px-6 py-10 text-center transition-colors",
                isDragging ? "border-primary bg-primary/5" : "border-border",
              )}
              onClick={() => fileInputRef.current?.click()}
              onDragEnter={(event) => {
                event.preventDefault();
                setIsDragging(true);
              }}
              onDragLeave={(event) => {
                event.preventDefault();
                if (event.currentTarget === event.target) {
                  setIsDragging(false);
                }
              }}
              onDragOver={(event) => event.preventDefault()}
              onDrop={handleDrop}
            >
              <Upload className="mb-4 h-12 w-12 text-muted-foreground" />
              <p className="text-lg text-muted-foreground">{Texts.importDropFiles}</p>
            </button>

            {uploadItems.length > 0 && (
              <div className="h-64 overflow-y-auto rounded-md border p-3">
                <ul className="space-y-2">
                  {uploadItems.map((item) => {
                    const metadata = item.detection;
                    const period = formatPeriod(metadata?.year ?? null, metadata?.month ?? null);

                    return (
                      <li key={item.key} className="flex items-start justify-between gap-3 rounded-sm border px-3 py-2">
                        <div className="flex min-w-0 max-w-[18rem] flex-1 items-start gap-2 overflow-hidden">
                          <FileText className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                          <div className="min-w-0 flex-1">
                            <span className="block min-w-0 truncate text-sm font-medium" title={item.file.name}>
                              {item.file.name}
                            </span>
                            <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                              <UploadItemStatusIcon status={item.status} />
                              <span>{getSelectionStatusLabel(item.status, metadata)}</span>
                            </div>
                            {metadata?.employeeName && (
                              <div className="mt-1 text-xs text-muted-foreground">
                                {Texts.importEmployeeLabel}: {metadata.employeeName}
                              </div>
                            )}
                            {metadata?.employeePersonalNumber && (
                              <div className="mt-1 text-xs text-muted-foreground">
                                {Texts.importPersonalNumberLabel}: {metadata.employeePersonalNumber}
                              </div>
                            )}
                            {period && (
                              <div className="mt-1 text-xs text-muted-foreground">
                                {Texts.importPeriodLabel}: {period}
                              </div>
                            )}
                            {metadata?.errorMessage && <div className="mt-1 text-xs text-destructive">{metadata.errorMessage}</div>}
                          </div>
                        </div>
                        <DeleteButton onClick={() => handleRemoveFile(item.file)} disabled={isImporting} />
                      </li>
                    );
                  })}
                </ul>
              </div>
            )}
          </div>
        ) : (
          <div className="h-80 overflow-y-auto rounded-md border p-3">
            <ul className="space-y-2">
              {uploadItems.map((item) => {
                const metadata = item.detection;
                const period = formatPeriod(item.result?.year ?? metadata?.year ?? null, item.result?.month ?? metadata?.month ?? null);

                return (
                  <li key={item.key} className="rounded-sm border px-3 py-2">
                    <div className="flex min-w-0 items-start gap-2 overflow-hidden">
                      <FileText className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <span className="block min-w-0 truncate text-sm font-medium" title={item.file.name}>
                            {item.file.name}
                          </span>
                        </div>
                        <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                          <UploadItemStatusIcon status={item.status} />
                          <span>{getResultStatusLabel(item.status, item.result, metadata)}</span>
                        </div>
                        {metadata?.employeeName && (
                          <div className="mt-1 text-xs text-muted-foreground">
                            {Texts.importEmployeeLabel}: {metadata.employeeName}
                          </div>
                        )}
                        {metadata?.employeePersonalNumber && (
                          <div className="mt-1 text-xs text-muted-foreground">
                            {Texts.importPersonalNumberLabel}: {metadata.employeePersonalNumber}
                          </div>
                        )}
                        {period && (
                          <div className="mt-1 text-xs text-muted-foreground">
                            {Texts.importPeriodLabel}: {period}
                          </div>
                        )}
                        {(item.result?.errorMessage ?? metadata?.errorMessage) && <div className="mt-1 text-xs text-destructive">{item.result?.errorMessage ?? metadata?.errorMessage}</div>}
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>
          </div>
        )}

        <DialogFooter>
          {mode === "selection" ? (
            <>
              <DialogCancelButton onClick={handleClose} />
              <DialogConfirmButton disabled={uploadItems.length === 0 || readyItemsCount === 0 || isImporting || isDetecting} onClick={(_, signal) => handleImport(signal)} />
            </>
          ) : (
            <Button type="button" variant="outline" onClick={handleClose}>
              {Texts.close}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

const UploadItemStatusIcon = ({ status }: { status: UploadItemStatus }) => {
  if (status === "success") {
    return <CheckCircle2 className="h-3.5 w-3.5 shrink-0 text-green-600" />;
  }

  if (status === "error") {
    return <XCircle className="h-3.5 w-3.5 shrink-0 text-destructive" />;
  }

  if (status === "importing") {
    return <Loader2 className="h-3.5 w-3.5 shrink-0 animate-spin text-primary" />;
  }

  return <CircleDashed className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />;
};

const getSelectionStatusLabel = (status: UploadItemStatus, detection: TimesheetDetectionResult | null) => {
  if (status === "detecting") {
    return Texts.importDetecting;
  }

  if (status === "importing") {
    return Texts.importProcessing;
  }

  if (status === "invalid") {
    return Texts.importCannotImport;
  }

  if (status === "ready") {
    return detection?.isReimport ? Texts.importReimport : Texts.importReady;
  }

  return detection?.errorMessage ?? Texts.importError;
};

const getResultStatusLabel = (status: UploadItemStatus, result: ImportResult | null, detection: TimesheetDetectionResult | null) => {
  if (status === "success") {
    return Texts.importSucceeded;
  }

  if (status === "error" || status === "invalid") {
    return result?.errorMessage || detection?.errorMessage ? Texts.importFailedSingle : Texts.importError;
  }

  if (status === "importing") {
    return Texts.importProcessing;
  }

  if (status === "detecting") {
    return Texts.importDetecting;
  }

  return Texts.importReady;
};
