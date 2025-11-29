import { useState } from "react";
import { Dialog, DialogBody, DialogTitle, DialogContent, DialogActions, DialogCancelButton, DialogConfirmButton } from "../common/Dialog";
import { Texts } from "../common/Texts";

export interface ProjectFormData {
  name: string;
  registrationNumber: string;
  startDate: string;
  endDate: string;
  recipientName: string;
  description: string;
}

interface AddProjectDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (data: ProjectFormData) => void;
}

export const AddProjectDialog = ({ isOpen, onClose, onConfirm }: AddProjectDialogProps) => {
  const [formData, setFormData] = useState<ProjectFormData>({
    name: "",
    registrationNumber: "",
    startDate: "",
    endDate: "",
    recipientName: "",
    description: "",
  });

  const handleConfirm = () => onConfirm(formData);
  const handleCancel = () => onClose();

  const updateField = (field: keyof ProjectFormData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <Dialog isOpen={isOpen} onClose={handleCancel}>
      <DialogBody>
        <DialogTitle>{Texts.newProject}</DialogTitle>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.projectName} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => updateField("name", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.projectIdLabel} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.registrationNumber}
                onChange={(e) => updateField("registrationNumber", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.startDate} <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => updateField("startDate", e.target.value)}
                    className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400 pr-10"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.endDate} <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => updateField("endDate", e.target.value)}
                    className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400 pr-10"
                  />
                </div>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.recipientName} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.recipientName}
                onChange={(e) => updateField("recipientName", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.projectDescription}
              </label>
              <textarea
                value={formData.description}
                onChange={(e) => updateField("description", e.target.value)}
                rows={4}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400 resize-y"
              />
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