export interface EditableFieldProps<T> {
  value: T;
  onChange: (value: T) => void;
}

export interface ComputedFieldProps<T> {
  value: T;
}

export interface StaticFieldProps {
  label: string;
}
