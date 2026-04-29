# Mental Process & Development Workflow

## 1. Domain Mapping (The "Picture")
Before writing any code or proposing architecture, you must understand the business domain.
- **Identify Entities**: Identify the main domain elements (e.g., Project, Contract, Timesheet).
- **Establish Hierarchy & Relationships**: Determine who owns what (e.g., Project -> Contracts -> User Allocations).
- **Roles & Permissions**: Identify key stakeholders (e.g., ProjectManager, ContractManager).
- **Identify Gaps**: If the business logic is unclear, you MUST ask for clarification before proceeding.

## 2. Structural Design
- **Mental Modeling**: Create a hierarchical map of the domain.
- **Data Schema**: Propose a database schema (even for Code-First). Think in terms of relationships (1:N, M:N) and data integrity.
- **Verification**: Present the proposed domain model and schema to the user for approval before touching any application logic.

## 3. Implementation Path
- **API Design**: Once the domain is clear, design the API endpoints or service contracts.
- **Vertical Slice**: Implement the feature as a vertical slice (Domain -> Logic -> API/UI) to keep context together.