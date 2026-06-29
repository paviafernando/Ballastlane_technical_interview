import { apiFetch } from './apiClient'

export type TaskStatus = 'Pending' | 'InProgress' | 'Blocked' | 'Completed' | 'Cancelled'

export interface TaskDto {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
}

export interface CreateTaskRequest {
  title: string
  description: string | null
  status: TaskStatus | null
  dueDate: string | null
}

export interface UpdateTaskRequest {
  title: string
  description: string | null
  status: TaskStatus
  dueDate: string | null
}

export function getAllTasks(token: string): Promise<TaskDto[]> {
  return apiFetch<TaskDto[]>('/api/tasks', {}, token)
}

export function createTask(request: CreateTaskRequest, token: string): Promise<TaskDto> {
  return apiFetch<TaskDto>('/api/tasks', { method: 'POST', body: JSON.stringify(request) }, token)
}

export function updateTask(id: string, request: UpdateTaskRequest, token: string): Promise<TaskDto> {
  return apiFetch<TaskDto>(`/api/tasks/${id}`, { method: 'PUT', body: JSON.stringify(request) }, token)
}

export function deleteTask(id: string, token: string): Promise<void> {
  return apiFetch<void>(`/api/tasks/${id}`, { method: 'DELETE' }, token)
}
