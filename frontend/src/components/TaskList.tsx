import type { TaskDto, TaskStatus } from '../api/tasksApi'
import './TaskList.css'

const STATUS_VALUES: TaskStatus[] = ['Pending', 'InProgress', 'Blocked', 'Completed', 'Cancelled']

interface TaskListProps {
  tasks: TaskDto[]
  onEdit: (task: TaskDto) => void
  onDelete: (taskId: string) => void
  onStatusChange: (task: TaskDto, status: TaskStatus) => void
  onComplete?: (task: TaskDto) => void
  compact?: boolean
}

function statusBadgeClass(status: TaskStatus) {
  return `status-badge status-badge--${status.toLowerCase()}`
}

export function TaskList({ tasks, onEdit, onDelete, onStatusChange, onComplete, compact = false }: TaskListProps) {
  if (tasks.length === 0) {
    return <p className="task-list-empty">No tasks yet.</p>
  }

  const showComplete = Boolean(onComplete) && !compact

  return (
    <ul className={compact ? 'task-list task-list--compact' : 'task-list'}>
      {tasks.map((task) => (
        <li
          key={task.id}
          className={compact ? 'task-row task-row--compact' : 'task-row'}
          onClick={showComplete ? () => onComplete!(task) : undefined}
        >
          {showComplete && (
            <button
              type="button"
              className="task-row-checkbox"
              aria-label={`Mark "${task.title}" as completed`}
              onClick={(event) => {
                event.stopPropagation()
                onComplete!(task)
              }}
            />
          )}

          <div className="task-row-main">
            <span className="task-row-title">{task.title}</span>
            {!compact && task.dueDate && <span className="task-row-due">Due {task.dueDate}</span>}
          </div>

          <select
            aria-label="Task status"
            className={statusBadgeClass(task.status)}
            value={task.status}
            onClick={(event) => event.stopPropagation()}
            onChange={(event) => onStatusChange(task, event.target.value as TaskStatus)}
          >
            {STATUS_VALUES.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>

          <div className="task-row-actions" onClick={(event) => event.stopPropagation()}>
            <button className="task-row-edit" onClick={() => onEdit(task)}>
              Edit
            </button>
            <button className="task-row-delete" onClick={() => onDelete(task.id)}>
              Delete
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}
