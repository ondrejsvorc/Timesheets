import { Texts } from "../common/Texts";

export const EmployeesFilter = () => {
  return (
    <div className="flex items-center gap-4 flex-wrap">
      <input
        type="text"
        placeholder={Texts.searchByNameEmailOrNumber}
        className="border border-gray-300 px-3 py-2 rounded w-80"
      />
      <label className="flex items-center gap-2 text-sm text-gray-700">
        {Texts.employeesInMyContracts}
        <input type="checkbox" className="toggle toggle-sm" />
      </label>
    </div>
  );
};

