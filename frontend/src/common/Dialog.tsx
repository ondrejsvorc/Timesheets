import { X } from "lucide-react";
import { createContext, useContext, useEffect } from "react";
import type { ReactNode } from "react";
import { Texts } from "./Texts";

interface DialogContextType {
  isOpen: boolean;
  onClose: () => void;
}

const DialogContext = createContext<DialogContextType | null>(null);

const useDialogContext = () => {
  const context = useContext(DialogContext);
  if (!context) {
    throw new Error("Dialog components must be used within Dialog");
  }
  return context;
};

interface DialogProps {
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
}

export const Dialog = ({ isOpen, onClose, children }: DialogProps) => {
  // Prevent body scroll when dialog is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <DialogContext.Provider value={{ isOpen, onClose }}>
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm" onClick={handleBackdropClick}>
        {children}
      </div>
    </DialogContext.Provider>
  );
};

interface DialogBodyProps {
  children: ReactNode;
  className?: string;
}

export const DialogBody = ({ children, className = "" }: DialogBodyProps) => {
  const handleDialogClick = (e: React.MouseEvent<HTMLDivElement>) => {
    e.stopPropagation();
  };

  return (
    <div className={`bg-white rounded-lg shadow-xl w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto ${className}`} onClick={handleDialogClick}>
      {children}
    </div>
  );
};

interface DialogTitleProps {
  children: ReactNode;
}

export const DialogTitle = ({ children }: DialogTitleProps) => {
  const { onClose } = useDialogContext();

  return (
    <div className="flex items-center justify-between p-6">
      <h2 className="text-2xl font-semibold text-gray-800">{children}</h2>
      <button onClick={onClose} className="text-gray-500 hover:text-gray-700 transition-colors" aria-label="Close">
        <X className="w-5 h-5" />
      </button>
    </div>
  );
};

interface DialogContentProps {
  children: ReactNode;
  className?: string;
}

export const DialogContent = ({ children, className = "" }: DialogContentProps) => {
  return <div className={`p-6 ${className}`}>{children}</div>;
};

interface DialogActionsProps {
  children: ReactNode;
  className?: string;
}

export const DialogActions = ({ children, className = "" }: DialogActionsProps) => {
  return (
    <div className={`flex items-center justify-end gap-3 p-6 ${className}`}>
      {children}
    </div>
  );
};

interface DialogCancelButtonProps {
  onClick?: () => void;
  label?: string;
}

export const DialogCancelButton = ({ onClick, label = Texts.cancel }: DialogCancelButtonProps) => {
  const { onClose } = useDialogContext();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      onClose();
    }
  };

  return (
    <button onClick={handleClick} className="px-4 py-2 bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors">
      {label}
    </button>
  );
};

interface DialogConfirmButtonProps {
  onClick: () => void;
  label?: string;
  disabled?: boolean;
}

export const DialogConfirmButton = ({
  onClick,
  label = Texts.confirm,
  disabled = false,
}: DialogConfirmButtonProps) => {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="px-4 py-2 bg-gray-900 text-white rounded hover:bg-gray-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
    >
      {label}
    </button>
  );
};
