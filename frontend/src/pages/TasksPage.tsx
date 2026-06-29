import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createTask, deleteTask, getAllTasks, updateTask, type TaskDto, type TaskStatus } from '../api/tasksApi'
import { ApiError } from '../api/apiClient'
import { useAuth } from '../auth/useAuth'
import { Modal } from '../components/Modal'
import { TaskForm, type TaskFormValues } from '../components/TaskForm'
import { TaskList } from '../components/TaskList'
import './TasksPage.css'

function toFormValues(task: TaskDto): TaskFormValues {
  return {
    title: task.title,
    description: task.description ?? '',
    status: task.status,
    dueDate: task.dueDate ?? '',
  }
}

const ACTIVE_STATUSES: TaskStatus[] = ['Pending', 'InProgress', 'Blocked']

type ModalState = { mode: 'create' } | { mode: 'edit'; task: TaskDto } | null

export function TasksPage() {
  const { username, token, logout } = useAuth()
  const navigate = useNavigate()

  const [tasks, setTasks] = useState<TaskDto[]>([])
  const [modal, setModal] = useState<ModalState>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      return
    }

    getAllTasks(token)
      .then(setTasks)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load tasks.'))
  }, [token])

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const handleCreate = async (values: TaskFormValues) => {
    if (!token) return
    setError(null)
    try {
      const created = await createTask(
        { title: values.title, description: values.description, status: values.status, dueDate: values.dueDate || null },
        token,
      )
      setTasks((current) => [...current, created])
      setModal(null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to create task.')
    }
  }

  const handleUpdate = async (task: TaskDto, values: TaskFormValues) => {
    if (!token) return
    setError(null)
    try {
      const updated = await updateTask(
        task.id,
        { title: values.title, description: values.description, status: values.status, dueDate: values.dueDate || null },
        token,
      )
      setTasks((current) => current.map((t) => (t.id === updated.id ? updated : t)))
      setModal(null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update task.')
    }
  }

  const handleStatusChange = async (task: TaskDto, status: TaskStatus) => {
    if (!token) return
    setError(null)
    try {
      const updated = await updateTask(
        task.id,
        { title: task.title, description: task.description, status, dueDate: task.dueDate },
        token,
      )
      setTasks((current) => current.map((t) => (t.id === updated.id ? updated : t)))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update task.')
    }
  }

  const handleDelete = async (taskId: string) => {
    if (!token) return
    setError(null)
    try {
      await deleteTask(taskId, token)
      setTasks((current) => current.filter((task) => task.id !== taskId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to delete task.')
    }
  }

  const openEdit = (task: TaskDto) => setModal({ mode: 'edit', task })
  const closeModal = () => setModal(null)

  const activeTasks = tasks.filter((task) => ACTIVE_STATUSES.includes(task.status))
  const completedTasks = tasks.filter((task) => task.status === 'Completed')
  const cancelledTasks = tasks.filter((task) => task.status === 'Cancelled')

  return (
    <div className="tasks-page">
      <header className="tasks-topbar">
        <span className="tasks-topbar-user">
          Welcome, <strong>{username}</strong>
        </span>
        <button className="logout-button" onClick={handleLogout}>
          Log out
        </button>
      </header>

      <div className="tasks-content">
        <div className="tasks-content-header">
          <h1>Tasks</h1>
          <button className="new-task-button" onClick={() => setModal({ mode: 'create' })}>
            New task
          </button>
        </div>

        {error && (
          <p role="alert" className="form-error">
            {error}
          </p>
        )}

        <TaskList
          tasks={activeTasks}
          onEdit={openEdit}
          onDelete={handleDelete}
          onStatusChange={handleStatusChange}
          onComplete={(task) => handleStatusChange(task, 'Completed')}
        />

        {completedTasks.length > 0 && (
          <section className="tasks-section-compact">
            <h2 className="tasks-section-title">Completed ({completedTasks.length})</h2>
            <TaskList
              compact
              tasks={completedTasks}
              onEdit={openEdit}
              onDelete={handleDelete}
              onStatusChange={handleStatusChange}
            />
          </section>
        )}

        {cancelledTasks.length > 0 && (
          <section className="tasks-section-compact">
            <h2 className="tasks-section-title">Cancelled ({cancelledTasks.length})</h2>
            <TaskList
              compact
              tasks={cancelledTasks}
              onEdit={openEdit}
              onDelete={handleDelete}
              onStatusChange={handleStatusChange}
            />
          </section>
        )}
      </div>

      {modal && (
        <Modal title={modal.mode === 'create' ? 'New task' : 'Edit task'} onClose={closeModal}>
          {modal.mode === 'create' ? (
            <TaskForm submitLabel="Create task" onSubmit={handleCreate} onCancel={closeModal} />
          ) : (
            <TaskForm
              submitLabel="Save changes"
              initialValues={toFormValues(modal.task)}
              onSubmit={(values) => handleUpdate(modal.task, values)}
              onCancel={closeModal}
            />
          )}
        </Modal>
      )}
    </div>
  )
}
