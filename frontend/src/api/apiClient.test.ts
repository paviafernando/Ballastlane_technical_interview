import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiFetch } from './apiClient'

function mockFetchResponse(body: unknown, status: number) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  })
}

describe('apiFetch', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns the parsed JSON body on success', async () => {
    vi.stubGlobal('fetch', mockFetchResponse({ value: 42 }, 200))

    const result = await apiFetch<{ value: number }>('/api/things')

    expect(result).toEqual({ value: 42 })
  })

  it('sends a JSON content-type header and the request body', async () => {
    const fetchMock = mockFetchResponse({}, 200)
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/api/things', { method: 'POST', body: JSON.stringify({ a: 1 }) })

    const [, options] = fetchMock.mock.calls[0]
    const headers = options.headers as Headers
    expect(headers.get('Content-Type')).toBe('application/json')
    expect(options.body).toBe(JSON.stringify({ a: 1 }))
  })

  it('attaches an Authorization header when a token is provided', async () => {
    const fetchMock = mockFetchResponse({}, 200)
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/api/things', {}, 'my-token')

    const [, options] = fetchMock.mock.calls[0]
    const headers = options.headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer my-token')
  })

  it('returns undefined for a 204 No Content response', async () => {
    vi.stubGlobal('fetch', mockFetchResponse(null, 204))

    const result = await apiFetch('/api/things', { method: 'DELETE' })

    expect(result).toBeUndefined()
  })

  it('throws an ApiError with the response message and status on failure', async () => {
    vi.stubGlobal('fetch', mockFetchResponse({ message: 'Invalid username or password.' }, 401))

    const error = await apiFetch('/api/auth/login').catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ message: 'Invalid username or password.', status: 401 })
  })
})
