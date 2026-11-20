import { http } from '../../../shared/api/http'

export interface ConceptoCobroItem {
  id: string
  clave: string
  descripcion: string
  cuotaBase: number
  orden?: number | null
  cuentaReferencia?: string | null
  aplicaBeca: boolean
  categoria: string
  isActive: boolean
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ConceptoFilterParams {
  search?: string
  categoria?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

export interface CreateConceptoCobroPayload {
  clave: string
  descripcion: string
  cuotaBase: number
  orden?: number | null
  cuentaReferencia?: string | null
  aplicaBeca: boolean
  categoria: string
}

export interface UpdateConceptoCobroPayload {
  descripcion: string
  cuotaBase: number
  orden?: number | null
  cuentaReferencia?: string | null
  aplicaBeca: boolean
  categoria: string
  isActive: boolean
}

/**
 * Fetch paginated and filtered list of payment concepts from .NET Backend
 */
export async function getConceptosPaged(params: ConceptoFilterParams = {}): Promise<PagedResult<ConceptoCobroItem>> {
  const { data } = await http.get<PagedResult<ConceptoCobroItem>>('/conceptos-cobro', { params })
  return data
}

/**
 * Fetch all active concepts unpaginated (for cashier dropdowns)
 */
export async function getConceptosAll(): Promise<ConceptoCobroItem[]> {
  const { data } = await http.get<ConceptoCobroItem[]>('/conceptos-cobro/all')
  return data
}

/**
 * Fetch list of unique category names from backend
 */
export async function getConceptosCategorias(): Promise<string[]> {
  const { data } = await http.get<string[]>('/conceptos-cobro/categories')
  return data
}

/**
 * Fetch a single concept by UUID
 */
export async function getConceptoById(id: string): Promise<ConceptoCobroItem> {
  const { data } = await http.get<ConceptoCobroItem>(`/conceptos-cobro/${id}`)
  return data
}

/**
 * Create a new payment concept
 */
export async function createConcepto(payload: CreateConceptoCobroPayload): Promise<ConceptoCobroItem> {
  const { data } = await http.post<ConceptoCobroItem>('/conceptos-cobro', payload)
  return data
}

/**
 * Update an existing payment concept
 */
export async function updateConcepto(id: string, payload: UpdateConceptoCobroPayload): Promise<ConceptoCobroItem> {
  const { data } = await http.put<ConceptoCobroItem>(`/conceptos-cobro/${id}`, payload)
  return data
}

/**
 * Toggle active/inactive status of a concept
 */
export async function toggleStatusConcepto(id: string): Promise<ConceptoCobroItem> {
  const { data } = await http.patch<ConceptoCobroItem>(`/conceptos-cobro/${id}/toggle-status`)
  return data
}

/**
 * Delete or soft-delete a payment concept
 */
export async function deleteConcepto(id: string): Promise<void> {
  await http.delete(`/conceptos-cobro/${id}`)
}
