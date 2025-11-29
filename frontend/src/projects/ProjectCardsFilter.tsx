import { Texts } from "../common/Texts";

export const ProjectCardsFilter = () => {
  return (
    <div className="flex items-center gap-4 flex-wrap">
      <input
        type="text"
        placeholder={Texts.searchByNameOrId}
        className="border border-gray-300 px-3 py-2 rounded w-60"
      />

      <input type="date" className="border border-gray-300 px-3 py-2 rounded w-32" />
      <input type="date" className="border border-gray-300 px-3 py-2 rounded w-32" />

      <button className="px-3 py-2 border border-gray-300 rounded hover:bg-gray-100">
        {Texts.removeFilter}
      </button>

      <label className="flex items-center gap-2 text-sm text-gray-700">
        {Texts.activeOnly}
        <input type="checkbox" className="toggle toggle-sm" />
      </label>
    </div>
  );
};

