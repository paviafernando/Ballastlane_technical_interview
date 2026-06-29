import { useState, type FormEvent } from 'react'
import type { TaskStatus } from '../api/tasksApi'
import { validateTaskForm, type TaskFormErrors } from '../tasks/validation'
import './TaskForm.css'

export interface TaskFormValues {
  title: string
  description: string
  status: TaskStatus
  dueDate: string
}

const STATUS_OPTIONS: { value: TaskStatus; label: string }[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Blocked', label: 'Blocked' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Cancelled', label: 'Cancelled' },
]

const DEFAULT_VALUES: TaskFormValues = { title: '', description: '', status: 'Pending', dueDate: '' }

interface TaskFormProps {
  submitLabel: string
  initialValues?: TaskFormValues
  onSubmit: (values: TaskFormValues) => void
  onCancel?: () => void
}

export function TaskForm({ submitLabel, initialValues, onSubmit, onCancel }: TaskFormProps) {
  const [values, setValues] = useState<TaskFormValues>(initialValues ?? DEFAULT_VALUES)
  const [errors, setErrors] = useState<TaskFormErrors>({})

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const validationErrors = validateTaskForm(values)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      return
    }

    onSubmit(values)
  }

  return (
    <form className="task-form" onSubmit={handleSubmit}>
      <div className="task-form-grid">
        <div className="task-form-field span-2">
          <label htmlFor="task-title">Title</label>
          <input
            id="task-title"
            value={values.title}
            onChange={(e) => setValues({ ...values, title: e.target.value })}
          />
          {errors.title && (
            <p role="alert" className="field-error">
              {errors.title}
            </p>
          )}
        </div>

        <div className="task-form-field span-2">
          <label htmlFor="task-description">Description</label>
          <textarea
            id="task-description"
            value={values.description}
            onChange={(e) => setValues({ ...values, description: e.target.value })}
          />
        </div>

        <div className="task-form-field">
          <label htmlFor="task-status">Status</label>
          <select
            id="task-status"
            value={values.status}
            onChange={(e) => setValues({ ...values, status: e.target.value as TaskStatus })}
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>

        <div className="task-form-field">
          <label htmlFor="task-due-date">Due date</label>
          <input
            id="task-due-date"
            type="date"
            value={values.dueDate}
            onChange={(e) => setValues({ ...values, dueDate: e.target.value })}
          />
        </div>
      </div>

      <div className="task-form-actions">
        {onCancel && (
          <button type="button" className="task-form-cancel" onClick={onCancel}>
            Cancel
          </button>
        )}
        <button type="submit" className="task-form-submit">
          {submitLabel}
        </button>
      </div>
    </form>
  )
}
