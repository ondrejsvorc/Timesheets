import type { Dispatch } from "react";
import { useState } from "react";
import { useAsyncValue, useFetcher, useLoaderData, useParams } from "react-router";
import { useImmerReducer } from "use-immer";
import { UiAction } from "@/auth/uiPermissions";
import { useCan } from "@/auth/useCan";
import { ActionButtons, AddButton, DeleteButton, EditButton } from "@/components/shared/buttons/ActionButtons";
import { EmptyState } from "@/components/shared/data/EmptyState";
import { AwaitContent } from "@/components/shared/layout/AwaitContent";
import { FilterBar } from "@/components/shared/layout/FilterBar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";
import { useNavigateFrom } from "@/hooks/useNavigateFrom";
import { createFilterControls } from "@/utils/createFilterControls";
import { AddContractDialog } from "./AddContractDialog";
import type { DeleteContractImpactResponse } from "./api/contractDeleteImpact";
import type { GetProjectContractsResponse } from "./api/getProjectContracts";
import type { ProjectContractItem } from "./api/shared/projectContractItem";
import { ContractDeleteDialog } from "./ContractDeleteDialog";
import { EditContractDialog } from "./EditContractDialog";
import { type ContractsFilterCriteria, useContractsFilter } from "./hooks/useContractsFilter";
import { type ProjectContractsAction, projectContractsReducer } from "./utils/projectContractsReducer";

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
  const [state, dispatch] = useImmerReducer(projectContractsReducer, response.projectContracts);
  const { filter, setFilter, filtered } = useContractsFilter(state);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const canAddContract = useCan(UiAction.contracts.add);

  return (
    <>
      <FilterBar
        filter={filter}
        setFilter={setFilter}
        actions={canAddContract ? <AddButton onClick={() => setIsAddOpen(true)}>{Texts.addContract}</AddButton> : undefined}
      >
        <FilterSearchInput placeholder={Texts.search} />
      </FilterBar>
      <ContractsTable contracts={filtered} dispatch={dispatch} />
      <AddContractDialog
        open={isAddOpen}
        projectId={projectId ?? ""}
        onClose={() => setIsAddOpen(false)}
        onSaved={(contract) => {
          dispatch({ type: "add", contract });
          setIsAddOpen(false);
        }}
      />
    </>
  );
};

interface ContractsTableProps {
  contracts: ProjectContractItem[];
  dispatch: Dispatch<ProjectContractsAction>;
}

export const ContractsTable = ({ contracts, dispatch }: ContractsTableProps) => {
  const { id: projectId } = useParams<{ id: string }>();
  const [contractToEdit, setContractToEdit] = useState<ProjectContractItem | null>(null);
  const [contractToDelete, setContractToDelete] = useState<ProjectContractItem | null>(null);
  const deleteImpactFetcher = useFetcher<DeleteContractImpactResponse>();

  const handleDeleteRequest = (contract: ProjectContractItem) => {
    deleteImpactFetcher.load(Routes.resourceContractDeleteImpact(contract.id));
    setContractToDelete(contract);
  };

  if (contracts.length === 0) {
    return <EmptyState />;
  }

  return (
    <>
      <div className="rounded-md border p-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{Texts.contractId}</TableHead>
              <TableHead>{Texts.contractName}</TableHead>
              <TableHead>{Texts.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {contracts.map((contract) => (
              <ContractRow key={contract.id} contract={contract} onEdit={setContractToEdit} onDelete={handleDeleteRequest} />
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
            dispatch({ type: "edit", contract: updatedContract });
            setContractToEdit(null);
          }}
        />
      )}

      {contractToDelete && projectId && (
        <ContractDeleteDialog
          projectId={projectId}
          contractId={contractToDelete.id}
          contractName={contractToDelete.name}
          fetcher={deleteImpactFetcher}
          onClose={() => setContractToDelete(null)}
          onDeleted={() => {
            dispatch({ type: "delete", contractId: contractToDelete.id });
            setContractToDelete(null);
          }}
        />
      )}
    </>
  );
};

interface ContractRowProps {
  contract: ProjectContractItem;
  onEdit: (contract: ProjectContractItem) => void;
  onDelete: (contract: ProjectContractItem) => void;
}

export const ContractRow = ({ contract, onEdit, onDelete }: ContractRowProps) => {
  const navigate = useNavigateFrom();
  const projectId = useParams().id;
  const canEdit = useCan(UiAction.contracts.edit);
  const canDelete = useCan(UiAction.contracts.delete);

  return (
    <TableRow className="cursor-pointer" onClick={() => projectId && navigate(Routes.contract(projectId, contract.id))}>
      <TableCell>{contract.registrationNumber}</TableCell>
      <TableCell>{contract.name}</TableCell>
      <TableCell>
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
                  onDelete(contract);
                }}
              />
            )}
          </ActionButtons>
        )}
      </TableCell>
    </TableRow>
  );
};
