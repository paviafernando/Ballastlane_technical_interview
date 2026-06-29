export interface TaskFormValues {
  title: string
  description: string
  dueDate: string
}

export type TaskFormErrors = Partial<Record<keyof TaskFormValues, string>>

export function validateTaskForm(values: TaskFormValues): TaskFormErrors {
  const errors: TaskFormErrors = {}

  if (!values.title.trim()) {
    errors.title = 'Title is required.'
  }

  return errors
}
