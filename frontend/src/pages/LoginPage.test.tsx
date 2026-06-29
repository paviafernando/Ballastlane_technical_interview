import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../api/authApi'
import { ApiError } from '../api/apiClient'
import { AuthProvider } from '../auth/AuthContext'
import { LoginPage } from './LoginPage'

vi.mock('../api/authApi')

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/tasks" element={<div>Tasks Page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.resetAllMocks()
  })

  it('renders username and password fields and a submit button', () => {
    renderLoginPage()

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /log in/i })).toBeInTheDocument()
  })

  it('shows validation errors and does not call the API when submitted empty', async () => {
    const user = userEvent.setup()
    renderLoginPage()

    await user.click(screen.getByRole('button', { name: /log in/i }))

    expect(await screen.findByText('Username is required.')).toBeInTheDocument()
    expect(screen.getByText('Password is required.')).toBeInTheDocument()
    expect(authApi.login).not.toHaveBeenCalled()
  })

  it('logs in and navigates to /tasks on valid credentials', async () => {
    vi.mocked(authApi.login).mockResolvedValue({ token: 'a.b.c', expiresAtUtc: '2026-01-01T00:00:00Z' })
    const user = userEvent.setup()
    renderLoginPage()

    await user.type(screen.getByLabelText(/username/i), 'janedoe')
    await user.type(screen.getByLabelText(/password/i), 'supersecret1')
    await user.click(screen.getByRole('button', { name: /log in/i }))

    expect(await screen.findByText('Tasks Page')).toBeInTheDocument()
  })

  it('shows an error message when the API rejects the login', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new ApiError('Invalid username or password.', 401))
    const user = userEvent.setup()
    renderLoginPage()

    await user.type(screen.getByLabelText(/username/i), 'janedoe')
    await user.type(screen.getByLabelText(/password/i), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /log in/i }))

    expect(await screen.findByText('Invalid username or password.')).toBeInTheDocument()
  })

  it('shows the account-locked message when the account is locked', async () => {
    vi.mocked(authApi.login).mockRejectedValue(
      new ApiError('Account is locked until 2026-01-01T12:05:00Z (UTC).', 423),
    )
    const user = userEvent.setup()
    renderLoginPage()

    await user.type(screen.getByLabelText(/username/i), 'janedoe')
    await user.type(screen.getByLabelText(/password/i), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /log in/i }))

    await waitFor(() =>
      expect(screen.getByText('Account is locked until 2026-01-01T12:05:00Z (UTC).')).toBeInTheDocument(),
    )
  })
})
