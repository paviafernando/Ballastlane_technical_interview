import { useState, type ReactNode } from 'react'
import { login as loginRequest } from '../api/authApi'
import { AuthContext, type AuthContextValue } from './authContext.context'

const STORAGE_KEY = 'taskManagementSystem.auth'

interface StoredAuth {
  token: string
  username: string
}

function readStoredAuth(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(() => readStoredAuth())

  const login = async (username: string, password: string) => {
    const response = await loginRequest(username, password)
    const newAuth: StoredAuth = { token: response.token, username }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(newAuth))
    setAuth(newAuth)
  }

  const logout = () => {
    localStorage.removeItem(STORAGE_KEY)
    setAuth(null)
  }

  const value: AuthContextValue = {
    token: auth?.token ?? null,
    username: auth?.username ?? null,
    isAuthenticated: auth !== null,
    login,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
