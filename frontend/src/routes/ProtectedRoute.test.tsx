import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import { ProtectedRoute } from './ProtectedRoute'

function renderWithAuth(initialPath: string, storedAuth?: { token: string; username: string }) {
  if (storedAuth) {
    localStorage.setItem('taskManagementSystem.auth', JSON.stringify(storedAuth))
  } else {
    localStorage.clear()
  }

  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/tasks" element={<div>Tasks Page</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('ProtectedRoute', () => {
  it('redirects to /login when there is no authenticated session', () => {
    renderWithAuth('/tasks')

    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })

  it('renders the protected content when authenticated', () => {
    renderWithAuth('/tasks', { token: 'a.b.c', username: 'janedoe' })

    expect(screen.getByText('Tasks Page')).toBeInTheDocument()
  })
})
