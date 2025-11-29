import { useState } from "react";
import { Dialog, DialogBody, DialogTitle, DialogContent, DialogActions, DialogCancelButton, DialogConfirmButton} from "../common/Dialog";
import { Texts } from "../common/Texts";

export interface EmployeePositionFormData {
  projectId: string;
  contractId: string;
  positionCode: string;
  positionName: string;
  workload: string;
  startDate: string;
  endDate: string | null;
}

interface AddEmployeePositionDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (data: EmployeePositionFormData) => void;
}

export const AddEmployeePositionDialog = ({
  isOpen,
  onClose,
  onConfirm,
}: AddEmployeePositionDialogProps) => {
  const [formData, setFormData] = useState<EmployeePositionFormData>({
    projectId: "",
    contractId: "",
    positionCode: "",
    positionName: "",
    workload: "",
    startDate: "",
    endDate: null,
  });

  const handleConfirm = () => {
    onConfirm(formData);
    setFormData({
      projectId: "",
      contractId: "",
      positionCode: "",
      positionName: "",
      workload: "",
      startDate: "",
      endDate: null,
    });
    onClose();
  };

  const handleCancel = () => {
    setFormData({
      projectId: "",
      contractId: "",
      positionCode: "",
      positionName: "",
      workload: "",
      startDate: "",
      endDate: null,
    });
    onClose();
  };

  const updateField = (field: keyof EmployeePositionFormData, value: string | null) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  return (
    <Dialog isOpen={isOpen} onClose={handleCancel}>
      <DialogBody>
        <DialogTitle>{Texts.addEmployeePosition}</DialogTitle>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.project} <span className="text-red-500">*</span>
              </label>
              <select
                value={formData.projectId}
                onChange={(e) => updateField("projectId", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              >
                <option value="">Value</option>
                {/* TODO: Load projects from API */}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.contract} <span className="text-red-500">*</span>
              </label>
              <select
                value={formData.contractId}
                onChange={(e) => updateField("contractId", e.target.value)}
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
                disabled={!formData.projectId}
              >
                <option value="">Value</option>
                {/* TODO: Load contracts from API based on projectId */}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.positionCode} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formData.positionCode}
                  onChange={(e) => updateField("positionCode", e.target.value)}
                  placeholder="Value"
                  className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.positionName} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formData.positionName}
                  onChange={(e) => updateField("positionName", e.target.value)}
                  placeholder="Value"
                  className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {Texts.workload} <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.workload}
                onChange={(e) => updateField("workload", e.target.value)}
                placeholder="Value"
                className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.startDate} <span className="text-red-500">*</span>
                </label>
                <input
                  type="date"
                  value={formData.startDate}
                  onChange={(e) => updateField("startDate", e.target.value)}
                  placeholder="Value"
                  className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {Texts.endDate}
                </label>
                <input
                  type="date"
                  value={formData.endDate || ""}
                  onChange={(e) => updateField("endDate", e.target.value || null)}
                  placeholder="Value"
                  className="w-full border border-gray-300 px-3 py-2 rounded focus:outline-none focus:ring-2 focus:ring-gray-400"
                />
              </div>
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

