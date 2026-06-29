import { apiFetch } from './apiClient'

export interface LoginResponse {
  token: string
  expiresAtUtc: string
}

export function login(username: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}

export interface RegisterRequest {
  name: string
  lastName: string
  username: string
  password: string
  birthday: string | null
}

export interface UserResponse {
  id: string
  name: string
  lastName: string
  username: string
  birthday: string | null
}

export function register(request: RegisterRequest): Promise<UserResponse> {
  return apiFetch<UserResponse>('/api/users', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
