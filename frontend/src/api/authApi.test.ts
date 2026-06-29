import { describe, expect, it, vi } from 'vitest'
import { apiFetch } from './apiClient'
import { login, register } from './authApi'

vi.mock('./apiClient', () => ({
  apiFetch: vi.fn(),
}))

describe('authApi', () => {
  it('login posts credentials to /api/auth/login and returns the token', async () => {
    vi.mocked(apiFetch).mockResolvedValue({ token: 'a.b.c', expiresAtUtc: '2026-01-01T00:00:00Z' })

    const result = await login('janedoe', 'supersecret1')

    expect(apiFetch).toHaveBeenCalledWith('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username: 'janedoe', password: 'supersecret1' }),
    })
    expect(result.token).toBe('a.b.c')
  })

  it('register posts the new user payload to /api/users', async () => {
    vi.mocked(apiFetch).mockResolvedValue({
      id: '1',
      name: 'Jane',
      lastName: 'Doe',
      username: 'janedoe',
      birthday: null,
    })

    const request = { name: 'Jane', lastName: 'Doe', username: 'janedoe', password: 'supersecret1', birthday: null }
    const result = await register(request)

    expect(apiFetch).toHaveBeenCalledWith('/api/users', {
      method: 'POST',
      body: JSON.stringify(request),
    })
    expect(result.username).toBe('janedoe')
  })
})
