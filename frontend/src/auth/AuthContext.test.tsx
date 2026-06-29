import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../api/authApi'
import { AuthProvider } from './AuthContext'
import { useAuth } from './useAuth'

vi.mock('../api/authApi')

const STORAGE_KEY = 'taskManagementSystem.auth'

function TestConsumer() {
  const { isAuthenticated, username, login, logout } = useAuth()

  return (
    <div>
      <span data-testid="status">{isAuthenticated ? `logged-in:${username}` : 'logged-out'}</span>
      <button onClick={() => login('janedoe', 'supersecret1')}>login</button>
      <button onClick={logout}>logout</button>
    </div>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.resetAllMocks()
  })

  it('starts logged out when there is no stored session', () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )

    expect(screen.getByTestId('status')).toHaveTextContent('logged-out')
  })

  it('logs in, exposes the username, and persists the session', async () => {
    vi.mocked(authApi.login).mockResolvedValue({ token: 'a.b.c', expiresAtUtc: '2026-01-01T00:00:00Z' })
    const user = userEvent.setup()

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )
    await user.click(screen.getByText('login'))

    await waitFor(() => expect(screen.getByTestId('status')).toHaveTextContent('logged-in:janedoe'))
    expect(JSON.parse(localStorage.getItem(STORAGE_KEY)!)).toMatchObject({
      token: 'a.b.c',
      username: 'janedoe',
    })
  })

  it('restores a previously persisted session on mount', () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ token: 'a.b.c', username: 'janedoe' }))

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )

    expect(screen.getByTestId('status')).toHaveTextContent('logged-in:janedoe')
  })

  it('logs out and clears the persisted session', async () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ token: 'a.b.c', username: 'janedoe' }))
    const user = userEvent.setup()

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )
    await act(async () => {
      await user.click(screen.getByText('logout'))
    })

    expect(screen.getByTestId('status')).toHaveTextContent('logged-out')
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull()
  })
})
