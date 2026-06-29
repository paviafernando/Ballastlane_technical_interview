import { describe, expect, it } from 'vitest'
import { validateTaskForm } from './validation'

describe('validateTaskForm', () => {
  it('returns no errors for valid input', () => {
    const errors = validateTaskForm({ title: 'Write report', description: '', dueDate: '' })

    expect(errors).toEqual({})
  })

  it('requires a title', () => {
    const errors = validateTaskForm({ title: '', description: '', dueDate: '' })

    expect(errors.title).toBe('Title is required.')
  })

  it('requires a title that is not just whitespace', () => {
    const errors = validateTaskForm({ title: '   ', description: '', dueDate: '' })

    expect(errors.title).toBe('Title is required.')
  })

  it('does not require a description or due date', () => {
    const errors = validateTaskForm({ title: 'Write report', description: '', dueDate: '' })

    expect(errors.description).toBeUndefined()
    expect(errors.dueDate).toBeUndefined()
  })
})
