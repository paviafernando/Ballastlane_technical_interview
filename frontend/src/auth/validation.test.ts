import { describe, expect, it } from 'vitest'
import { validateLoginForm } from './validation'

describe('validateLoginForm', () => {
  it('returns no errors for valid input', () => {
    const errors = validateLoginForm({ username: 'janedoe', password: 'supersecret1' })

    expect(errors).toEqual({})
  })

  it('requires a username', () => {
    const errors = validateLoginForm({ username: '', password: 'supersecret1' })

    expect(errors.username).toBe('Username is required.')
  })

  it('requires a username that is not just whitespace', () => {
    const errors = validateLoginForm({ username: '   ', password: 'supersecret1' })

    expect(errors.username).toBe('Username is required.')
  })

  it('requires a password', () => {
    const errors = validateLoginForm({ username: 'janedoe', password: '' })

    expect(errors.password).toBe('Password is required.')
  })

  it('requires a password of at least 8 characters', () => {
    const errors = validateLoginForm({ username: 'janedoe', password: 'short1' })

    expect(errors.password).toBe('Password must be at least 8 characters long.')
  })

  it('returns both errors when both fields are invalid', () => {
    const errors = validateLoginForm({ username: '', password: '' })

    expect(errors.username).toBeDefined()
    expect(errors.password).toBeDefined()
  })
})
