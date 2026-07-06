import type { Dispatch } from "react";
import { useState } from "react";
import { useAsyncValue, useLoaderData, useParams } from "react-router";
import { useImmerReducer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { ActionButtons, AddButton, DeleteButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { createFilterControls } from "@/components/shared/layout/createFilterControls";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useGo } from "@/hooks/useGo";
import { type ListCrudAction, listCrudReducer, listCrudState } from "@/utils/listCrudReducer";
import { AddContractDialog } from "./AddContractDialog";
import { deleteProjectContract, type GetProjectContractsResponse, type ProjectContractItem } from "./api";
import { EditContractDialog } from "./EditContractDialog";
import { type ContractsFilterCriteria, useContractsFilter } from "./hooks/useContractsFilter";

export const ProjectContracts = () => {
  const { promise } = useLoaderData() as {
    promise: Promise<GetProjectContractsResponse>;
  };

  return (
    <AwaitContent promise={promise}>
      <ProjectContractsContent />
    </AwaitContent>
  );
};

const { FilterSearchInput } = createFilterControls<ContractsFilterCriteria>();

const ProjectContractsContent = () => {
  const { id: projectId } = useParams<{ id: string }>();
  const response = useAsyncValue() as GetProjectContractsResponse;
  const [state, dispatch] = useImmerReducer(listCrudReducer, listCrudState(response.projectContracts));
  const { filter, setFilter, filtered } = useContractsFilter(state.items);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const canAddContract = useCan(UiAction.contracts.add, { projectId: projectId ?? undefined });

  return (
    <>
      <FilterBar filter={filter} setFilter={setFilter} actions={canAddContract ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addContract}</AddButton> : undefined}>
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsTable contracts={filtered} dispatch={dispatch} />
      <AddContractDialog
        open={isAddOpen}
        projectId={projectId ?? ""}
        onClose={() => setIsAddOpen(false)}
        onSaved={(contract) => {
          dispatch({ type: "add", item: contract });
          setIsAddOpen(false);
        }}
      />
      <ConfirmationDialog
        open={state.pendingDelete !== null}
        onCancel={() => dispatch({ type: "cancelDelete" })}
        onConfirm={async (_event, signal) => {
          if (!state.pendingDelete || !projectId) return;
          await deleteProjectContract(projectId, state.pendingDelete, signal);
          if (!signal.aborted) {
            dispatch({ type: "confirmDelete" });
          }
        }}
      />
    </>
  );
};

interface ContractsTableProps {
  contracts: ProjectContractItem[];
  dispatch: Dispatch<ListCrudAction<ProjectContractItem>>;
}

export const ContractsTable = ({ contracts, dispatch }: ContractsTableProps) => {
  const [contractToEdit, setContractToEdit] = useState<ProjectContractItem | null>(null);

  if (contracts.length === 0) {
    return <EmptyState />;
  }

  return (
    <>
      <div className="rounded-md border p-4">
        <Table className="table-fixed w-full">
          <TableHeader>
            <TableRow>
              <TableHead className="w-[22%]">{Texts.contractId}</TableHead>
              <TableHead className="w-[60%]">{Texts.contractName}</TableHead>
              <TableHead className="w-[18%] text-right">{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {contracts.map((contract) => (
              <ContractRow key={contract.id} contract={contract} onEdit={setContractToEdit} onRequestDelete={(id) => dispatch({ type: "requestDelete", key: id })} />
            ))}
          </TableBody>
        </Table>
      </div>

      {contractToEdit && (
        <EditContractDialog
          open
          contract={contractToEdit}
          onClose={() => setContractToEdit(null)}
          onSaved={(updatedContract) => {
            dispatch({ type: "update", item: updatedContract });
            setContractToEdit(null);
          }}
        />
      )}
    </>
  );
};

interface ContractRowProps {
  contract: ProjectContractItem;
  onEdit: (contract: ProjectContractItem) => void;
  onRequestDelete: (contractId: string) => void;
}

export const ContractRow = ({ contract, onEdit, onRequestDelete }: ContractRowProps) => {
  const go = useGo();
  const projectId = useParams().id;
  const canEdit = useCan(UiAction.contracts.edit, { projectId, contractId: contract.id });
  const canDelete = useCan(UiAction.contracts.delete, { projectId, contractId: contract.id });

  return (
    <TableRow className="cursor-pointer" onClick={() => projectId && go.forward(Routes.contract(projectId, contract.id))}>
      <TableCell className="truncate" title={contract.registrationNumber}>
        {contract.registrationNumber}
      </TableCell>
      <TableCell className="truncate" title={contract.name}>
        {contract.name}
      </TableCell>
      <TableCell>
        <div className="flex justify-end">
          {(canEdit || canDelete) && (
            <ActionButtons>
              {canEdit && (
                <EditButton
                  onClick={(e) => {
                    e.stopPropagation();
                    onEdit(contract);
                  }}
                />
              )}
              {canDelete && (
                <DeleteButton
                  onClick={(e) => {
                    e.stopPropagation();
                    onRequestDelete(contract.id);
                  }}
                />
              )}
            </ActionButtons>
          )}
        </div>
      </TableCell>
    </TableRow>
  );
};
