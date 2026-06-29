export interface LoginFormValues {
  username: string
  password: string
}

export type LoginFormErrors = Partial<Record<keyof LoginFormValues, string>>

const MINIMUM_PASSWORD_LENGTH = 8

export function validateLoginForm(values: LoginFormValues): LoginFormErrors {
  const errors: LoginFormErrors = {}

  if (!values.username.trim()) {
    errors.username = 'Username is required.'
  }

  if (!values.password) {
    errors.password = 'Password is required.'
  } else if (values.password.length < MINIMUM_PASSWORD_LENGTH) {
    errors.password = `Password must be at least ${MINIMUM_PASSWORD_LENGTH} characters long.`
  }

  return errors
}
