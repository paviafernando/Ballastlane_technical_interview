import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as tasksApi from '../api/tasksApi'
import type { TaskDto } from '../api/tasksApi'
import { AuthProvider } from '../auth/AuthContext'
import { TasksPage } from './TasksPage'

vi.mock('../api/tasksApi')

const TASKS: TaskDto[] = [
  { id: '1', title: 'Write report', description: 'Quarterly report', status: 'Pending', dueDate: '2026-02-01' },
]

function renderTasksPage() {
  localStorage.setItem('taskManagementSystem.auth', JSON.stringify({ token: 'a.b.c', username: 'janedoe' }))

  return render(
    <MemoryRouter initialEntries={['/tasks']}>
      <AuthProvider>
        <Routes>
          <Route path="/tasks" element={<TasksPage />} />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('TasksPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue([])
  })

  it('greets the logged-in user', async () => {
    renderTasksPage()

    expect(screen.getByText(/janedoe/)).toBeInTheDocument()
    await waitFor(() => expect(tasksApi.getAllTasks).toHaveBeenCalledWith('a.b.c'))
  })

  it('logs out and navigates to /login when the log out button is clicked', async () => {
    const user = userEvent.setup()
    renderTasksPage()

    await user.click(screen.getByRole('button', { name: /log out/i }))

    expect(await screen.findByText('Login Page')).toBeInTheDocument()
  })

  it('loads and displays the tasks for the current user', async () => {
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue(TASKS)
    renderTasksPage()

    expect(await screen.findByText('Write report')).toBeInTheDocument()
  })

  it('does not show the task form until "New task" is clicked', async () => {
    renderTasksPage()
    await waitFor(() => expect(tasksApi.getAllTasks).toHaveBeenCalled())

    expect(screen.queryByLabelText(/title/i)).not.toBeInTheDocument()
  })

  it('creates a new task from the New task modal and adds it to the list', async () => {
    const created: TaskDto = { id: '2', title: 'Plan launch', description: '', status: 'Pending', dueDate: null }
    vi.mocked(tasksApi.createTask).mockResolvedValue(created)
    const user = userEvent.setup()
    renderTasksPage()
    await waitFor(() => expect(tasksApi.getAllTasks).toHaveBeenCalled())

    await user.click(screen.getByRole('button', { name: /new task/i }))
    await user.type(screen.getByLabelText(/title/i), 'Plan launch')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByText('Plan launch')).toBeInTheDocument()
    expect(tasksApi.createTask).toHaveBeenCalledWith(
      { title: 'Plan launch', description: '', status: 'Pending', dueDate: null },
      'a.b.c',
    )
    expect(screen.queryByLabelText(/title/i)).not.toBeInTheDocument()
  })

  it('closes the modal without saving when Cancel is clicked', async () => {
    const user = userEvent.setup()
    renderTasksPage()
    await waitFor(() => expect(tasksApi.getAllTasks).toHaveBeenCalled())

    await user.click(screen.getByRole('button', { name: /new task/i }))
    expect(screen.getByLabelText(/title/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(screen.queryByLabelText(/title/i)).not.toBeInTheDocument()
    expect(tasksApi.createTask).not.toHaveBeenCalled()
  })

  it('edits a task via the Edit modal and updates it in the list', async () => {
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue(TASKS)
    const updated: TaskDto = { ...TASKS[0], title: 'Write final report' }
    vi.mocked(tasksApi.updateTask).mockResolvedValue(updated)
    const user = userEvent.setup()
    renderTasksPage()
    await screen.findByText('Write report')

    const row = screen.getByText('Write report').closest('li')!
    await user.click(within(row).getByRole('button', { name: /edit/i }))

    const titleInput = screen.getByLabelText(/title/i)
    await user.clear(titleInput)
    await user.type(titleInput, 'Write final report')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Write final report')).toBeInTheDocument()
    expect(tasksApi.updateTask).toHaveBeenCalledWith(
      '1',
      { title: 'Write final report', description: 'Quarterly report', status: 'Pending', dueDate: '2026-02-01' },
      'a.b.c',
    )
  })

  it('deletes a task and removes it from the list', async () => {
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue(TASKS)
    vi.mocked(tasksApi.deleteTask).mockResolvedValue(undefined)
    const user = userEvent.setup()
    renderTasksPage()
    const row = await screen.findByText('Write report')

    await user.click(within(row.closest('li')!).getByRole('button', { name: /delete/i }))

    await waitFor(() => expect(screen.queryByText('Write report')).not.toBeInTheDocument())
    expect(tasksApi.deleteTask).toHaveBeenCalledWith('1', 'a.b.c')
  })

  it('marks a task as completed when its row is clicked, moving it to the Completed section', async () => {
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue(TASKS)
    const completed: TaskDto = { ...TASKS[0], status: 'Completed' }
    vi.mocked(tasksApi.updateTask).mockResolvedValue(completed)
    const user = userEvent.setup()
    renderTasksPage()
    await screen.findByText('Write report')

    await user.click(screen.getByText('Write report'))

    expect(tasksApi.updateTask).toHaveBeenCalledWith(
      '1',
      { title: 'Write report', description: 'Quarterly report', status: 'Completed', dueDate: '2026-02-01' },
      'a.b.c',
    )
    expect(await screen.findByText('Completed (1)')).toBeInTheDocument()
  })

  it('changes a task status via the status select', async () => {
    vi.mocked(tasksApi.getAllTasks).mockResolvedValue(TASKS)
    const blocked: TaskDto = { ...TASKS[0], status: 'Blocked' }
    vi.mocked(tasksApi.updateTask).mockResolvedValue(blocked)
    const user = userEvent.setup()
    renderTasksPage()
    await screen.findByText('Write report')

    const row = screen.getByText('Write report').closest('li')!
    await user.selectOptions(within(row).getByRole('combobox', { name: /task status/i }), 'Blocked')

    expect(tasksApi.updateTask).toHaveBeenCalledWith(
      '1',
      { title: 'Write report', description: 'Quarterly report', status: 'Blocked', dueDate: '2026-02-01' },
      'a.b.c',
    )
  })
})
