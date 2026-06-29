import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { TaskDto } from '../api/tasksApi'
import { TaskList } from './TaskList'

const TASKS: TaskDto[] = [
  { id: '1', title: 'Write report', description: 'Quarterly report', status: 'Pending', dueDate: '2026-02-01' },
  { id: '2', title: 'Review PR', description: null, status: 'InProgress', dueDate: null },
]

describe('TaskList', () => {
  it('renders the title and status of each task', () => {
    render(<TaskList tasks={TASKS} onEdit={vi.fn()} onDelete={vi.fn()} onStatusChange={vi.fn()} />)

    expect(screen.getByText('Write report')).toBeInTheDocument()
    expect(screen.getByText('Review PR')).toBeInTheDocument()

    const firstRow = screen.getByText('Write report').closest('li')!
    const secondRow = screen.getByText('Review PR').closest('li')!
    expect(within(firstRow).getByRole('combobox', { name: /task status/i })).toHaveValue('Pending')
    expect(within(secondRow).getByRole('combobox', { name: /task status/i })).toHaveValue('InProgress')
  })

  it('shows an empty state message when there are no tasks', () => {
    render(<TaskList tasks={[]} onEdit={vi.fn()} onDelete={vi.fn()} onStatusChange={vi.fn()} />)

    expect(screen.getByText(/no tasks yet/i)).toBeInTheDocument()
  })

  it('calls onEdit with the task when its Edit button is clicked', async () => {
    const onEdit = vi.fn()
    const user = userEvent.setup()
    render(<TaskList tasks={TASKS} onEdit={onEdit} onDelete={vi.fn()} onStatusChange={vi.fn()} />)

    const firstRow = screen.getByText('Write report').closest('li')!
    await user.click(within(firstRow).getByRole('button', { name: /edit/i }))

    expect(onEdit).toHaveBeenCalledWith(TASKS[0])
  })

  it('calls onDelete with the task id when its Delete button is clicked', async () => {
    const onDelete = vi.fn()
    const user = userEvent.setup()
    render(<TaskList tasks={TASKS} onEdit={vi.fn()} onDelete={onDelete} onStatusChange={vi.fn()} />)

    const secondRow = screen.getByText('Review PR').closest('li')!
    await user.click(within(secondRow).getByRole('button', { name: /delete/i }))

    expect(onDelete).toHaveBeenCalledWith('2')
  })

  it('calls onStatusChange with the new status when the status select changes', async () => {
    const onStatusChange = vi.fn()
    const user = userEvent.setup()
    render(<TaskList tasks={TASKS} onEdit={vi.fn()} onDelete={vi.fn()} onStatusChange={onStatusChange} />)

    const firstRow = screen.getByText('Write report').closest('li')!
    await user.selectOptions(within(firstRow).getByRole('combobox', { name: /task status/i }), 'Blocked')

    expect(onStatusChange).toHaveBeenCalledWith(TASKS[0], 'Blocked')
  })

  it('calls onComplete when the row is clicked', async () => {
    const onComplete = vi.fn()
    const user = userEvent.setup()
    render(
      <TaskList tasks={TASKS} onEdit={vi.fn()} onDelete={vi.fn()} onStatusChange={vi.fn()} onComplete={onComplete} />,
    )

    await user.click(screen.getByText('Write report'))

    expect(onComplete).toHaveBeenCalledWith(TASKS[0])
  })

  it('calls onComplete when the checkbox button is clicked, without triggering it twice', async () => {
    const onComplete = vi.fn()
    const user = userEvent.setup()
    render(
      <TaskList tasks={TASKS} onEdit={vi.fn()} onDelete={vi.fn()} onStatusChange={vi.fn()} onComplete={onComplete} />,
    )

    const firstRow = screen.getByText('Write report').closest('li')!
    await user.click(within(firstRow).getByRole('button', { name: /mark "write report" as completed/i }))

    expect(onComplete).toHaveBeenCalledTimes(1)
    expect(onComplete).toHaveBeenCalledWith(TASKS[0])
  })

  it('does not render a completion checkbox or call onComplete when compact', async () => {
    const onComplete = vi.fn()
    const user = userEvent.setup()
    render(
      <TaskList
        compact
        tasks={TASKS}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
        onStatusChange={vi.fn()}
        onComplete={onComplete}
      />,
    )

    await user.click(screen.getByText('Write report'))

    expect(onComplete).not.toHaveBeenCalled()
  })
})
