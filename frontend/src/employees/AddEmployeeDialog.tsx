import { useState } from "react";
import {
  Dialog,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  DialogCancelButton,
  DialogConfirmButton,
} from "../common/Dialog";
import { Texts } from "../common/Texts";

export interface EmployeeFormData {
  personalNumber: string;
  fullName: string;
  email: string;
  employeeType: "Akademik" | "Neakademik";
}

interface AddEmployeeDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (data: EmployeeFormData) => void;
}

export const AddEmployeeDialog = ({
  isOpen,
  onClose,
  onConfirm,
}: AddEmployeeDialogProps) => {
  const [formData, setFormData] = useState<EmployeeFormData>({
    personalNumber: "",
    fullName: "",
    email: "",
    employeeType: "Neakademik",
  });

  const handleConfirm = () => {
    onConfirm(formData);
    setFormData({
      personalNumber: "",
      fullName: "",
      email: "",
      employeeType: "Neakademik",
    });
    onClose();
  };

  const handleCancel = () => {
    setFormData({
      personalNumber: "",
      fullName: "",
      email: "",
      employeeType: "Neakademik",
    });
    onClose();
  };

  const updateField = (
    field: keyof EmployeeFormData,
    value: string | "Akademik" | "Neakademik"
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <Dialog isOpen={isOpen} onClose={handleCancel}>
      <DialogBody>
        <DialogTitle>Nový zaměstnanec</DialogTitle>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.personalNumber} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.personalNumber}
                onChange={(e) => updateField("personalNumber", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.fullName} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.fullName}
                onChange={(e) => updateField("fullName", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.email} <span className="text-red-500">*</span>
              </label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => updateField("email", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.employeeType} <span className="text-red-500">*</span>
              </label>
              <select
                value={formData.employeeType}
                onChange={(e) =>
                  updateField("employeeType", e.target.value as "Akademik" | "Neakademik")
                }
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              >
                <option value="Neakademik">{Texts.nonAcademic}</option>
                <option value="Akademik">{Texts.academic}</option>
              </select>
            </div>
          </div>
        </DialogContent>
        <DialogActions>
          <DialogCancelButton onClick={handleCancel} label={Texts.cancel} />
          <DialogConfirmButton onClick={handleConfirm} label={Texts.confirm} />
        </DialogActions>
      </DialogBody>
    </Dialog>
  );
};

