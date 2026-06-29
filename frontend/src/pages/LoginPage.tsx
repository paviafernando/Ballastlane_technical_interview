import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api/apiClient'
import { useAuth } from '../auth/useAuth'
import { validateLoginForm, type LoginFormErrors } from '../auth/validation'
import './LoginPage.css'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<LoginFormErrors>({})
  const [apiError, setApiError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setApiError(null)

    const errors = validateLoginForm({ username, password })
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await login(username, password)
      navigate('/tasks')
    } catch (error) {
      setApiError(error instanceof ApiError ? error.message : 'Something went wrong. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Log in</h1>

        <div className="login-field">
          <label htmlFor="username">Username</label>
          <input id="username" value={username} onChange={(e) => setUsername(e.target.value)} />
          {fieldErrors.username && (
            <p role="alert" className="field-error">
              {fieldErrors.username}
            </p>
          )}
        </div>

        <div className="login-field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          {fieldErrors.password && (
            <p role="alert" className="field-error">
              {fieldErrors.password}
            </p>
          )}
        </div>

        {apiError && (
          <p role="alert" className="form-error">
            {apiError}
          </p>
        )}

        <button type="submit" className="login-submit" disabled={isSubmitting}>
          Log in
        </button>
      </form>
    </div>
  )
}
