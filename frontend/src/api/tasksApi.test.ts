import { describe, expect, it, vi } from 'vitest'
import { apiFetch } from './apiClient'
import { createTask, deleteTask, getAllTasks, updateTask } from './tasksApi'

vi.mock('./apiClient', () => ({
  apiFetch: vi.fn(),
}))

const TOKEN = 'a.b.c'

describe('tasksApi', () => {
  it('getAllTasks fetches /api/tasks with the auth token', async () => {
    const tasks = [{ id: '1', title: 'Write report', description: null, status: 'Pending', dueDate: null }]
    vi.mocked(apiFetch).mockResolvedValue(tasks)

    const result = await getAllTasks(TOKEN)

    expect(apiFetch).toHaveBeenCalledWith('/api/tasks', {}, TOKEN)
    expect(result).toEqual(tasks)
  })

  it('createTask posts the payload to /api/tasks with the auth token', async () => {
    const request = { title: 'Write report', description: null, status: null, dueDate: null }
    const created = { id: '1', ...request, status: 'Pending' }
    vi.mocked(apiFetch).mockResolvedValue(created)

    const result = await createTask(request, TOKEN)

    expect(apiFetch).toHaveBeenCalledWith(
      '/api/tasks',
      { method: 'POST', body: JSON.stringify(request) },
      TOKEN,
    )
    expect(result).toEqual(created)
  })

  it('updateTask puts the payload to /api/tasks/{id} with the auth token', async () => {
    const request = { title: 'Write final report', description: null, status: 'Completed' as const, dueDate: null }
    const updated = { id: '1', ...request }
    vi.mocked(apiFetch).mockResolvedValue(updated)

    const result = await updateTask('1', request, TOKEN)

    expect(apiFetch).toHaveBeenCalledWith(
      '/api/tasks/1',
      { method: 'PUT', body: JSON.stringify(request) },
      TOKEN,
    )
    expect(result).toEqual(updated)
  })

  it('deleteTask sends a DELETE to /api/tasks/{id} with the auth token', async () => {
    vi.mocked(apiFetch).mockResolvedValue(undefined)

    await deleteTask('1', TOKEN)

    expect(apiFetch).toHaveBeenCalledWith('/api/tasks/1', { method: 'DELETE' }, TOKEN)
  })
})
