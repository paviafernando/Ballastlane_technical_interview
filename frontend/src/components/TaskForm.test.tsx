import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { TaskForm } from './TaskForm'

describe('TaskForm', () => {
  it('renders title, description, status, and due date fields with the given submit label', () => {
    render(<TaskForm submitLabel="Create task" onSubmit={vi.fn()} />)

    expect(screen.getByLabelText(/title/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/description/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/status/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/due date/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Create task' })).toBeInTheDocument()
  })

  it('shows a validation error and does not submit when title is empty', async () => {
    const onSubmit = vi.fn()
    const user = userEvent.setup()
    render(<TaskForm submitLabel="Create task" onSubmit={onSubmit} />)

    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(await screen.findByText('Title is required.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits the entered values when valid', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup()
    render(<TaskForm submitLabel="Create task" onSubmit={onSubmit} />)

    await user.type(screen.getByLabelText(/title/i), 'Write report')
    await user.type(screen.getByLabelText(/description/i), 'Quarterly report')
    await user.selectOptions(screen.getByLabelText(/status/i), 'InProgress')
    await user.type(screen.getByLabelText(/due date/i), '2026-02-01')
    await user.click(screen.getByRole('button', { name: 'Create task' }))

    expect(onSubmit).toHaveBeenCalledWith({
      title: 'Write report',
      description: 'Quarterly report',
      status: 'InProgress',
      dueDate: '2026-02-01',
    })
  })

  it('pre-fills fields from initialValues for editing', () => {
    render(
      <TaskForm
        submitLabel="Save changes"
        initialValues={{ title: 'Existing task', description: 'Existing desc', status: 'Blocked', dueDate: '2026-03-01' }}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByLabelText(/title/i)).toHaveValue('Existing task')
    expect(screen.getByLabelText(/description/i)).toHaveValue('Existing desc')
    expect(screen.getByLabelText(/status/i)).toHaveValue('Blocked')
    expect(screen.getByLabelText(/due date/i)).toHaveValue('2026-03-01')
  })

  it('calls onCancel when the cancel button is clicked', async () => {
    const onCancel = vi.fn()
    const user = userEvent.setup()
    render(<TaskForm submitLabel="Save changes" onSubmit={vi.fn()} onCancel={onCancel} />)

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onCancel).toHaveBeenCalled()
  })
})
