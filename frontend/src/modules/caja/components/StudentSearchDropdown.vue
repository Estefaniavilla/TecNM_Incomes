<script setup lang="ts">
import type { AlumnoCaja } from '../types/caja'

withDefaults(
  defineProps<{
    sugerencias: AlumnoCaja[]
    visible?: boolean
  }>(),
  {
    visible: true,
  },
)

const emit = defineEmits<{
  select: [alumno: AlumnoCaja]
}>()

function getIniciales(nombre: string): string {
  if (!nombre) return '??'
  const partes = nombre.trim().split(/\s+/)
  if (partes.length >= 2) {
    return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
  }
  return nombre.substring(0, 2).toUpperCase()
}

function getColorAvatar(nombre: string): string {
  const colores = [
    '#1B396A', '#0F6B5E', '#8B5CF6', '#D97706',
    '#DC2626', '#0891B2', '#7C3AED', '#059669',
    '#E11D48', '#2563EB', '#CA8A04', '#9333EA',
  ]
  let hash = 0
  for (let i = 0; i < nombre.length; i++) {
    hash = nombre.charCodeAt(i) + ((hash << 5) - hash)
  }
  return colores[Math.abs(hash) % colores.length]
}

function getEstatusLabel(estatus: string): { texto: string; clase: string } {
  const s = (estatus || '').toLowerCase()
  if (s.includes('inscrito') && !s.includes('no')) return { texto: 'Al corriente', clase: 'badge-al-corriente' }
  if (s.includes('no inscrito') || s.includes('baja')) return { texto: 'Adeudo pendiente', clase: 'badge-adeudo' }
  if (s.includes('egresado')) return { texto: 'Egresado', clase: 'badge-egresado' }
  return { texto: estatus || 'Inscrito', clase: 'badge-default' }
}

function getNombreCarreraCorto(carrera: string): string {
  return (carrera || '').replace(/\s*\([^)]*\)/g, '').trim()
}
</script>

<template>
  <div v-if="sugerencias.length > 0 && visible" class="suggestions-dropdown">
    <!-- Encabezado de sugerencias -->
    <div class="suggestions-header">
      <span class="suggestions-count">
        <span class="dot-indicator"></span>
        Sugerencias encontradas ( {{ sugerencias.length }} )
      </span>
      <span class="suggestions-hint">Presiona Enter o haz click para seleccionar</span>
    </div>

    <!-- Lista de tarjetas de alumno -->
    <div class="suggestions-list">
      <div
        v-for="sug in sugerencias"
        :key="sug.matricula"
        class="suggestion-card"
        @mousedown.prevent="emit('select', sug)"
      >
        <!-- Avatar con iniciales -->
        <div
          class="sug-avatar"
          :style="{ backgroundColor: getColorAvatar(sug.nombreCompleto) }"
        >
          {{ getIniciales(sug.nombreCompleto) }}
        </div>

        <!-- Información principal -->
        <div class="sug-info">
          <div class="sug-top-row">
            <span class="sug-matricula">{{ sug.matricula }}</span>
            <span class="sug-nombre-completo">{{ sug.nombreCompleto }}</span>
          </div>
          <div class="sug-bottom-row">
            <span class="sug-carrera">{{ getNombreCarreraCorto(sug.carrera) }}</span>
            <span class="sug-separador">•</span>
            <span class="sug-semestre">{{ sug.semestre }}{{ sug.semestre === 1 ? 'er' : sug.semestre < 10 ? 'to' : 'vo' }} Semestre</span>
          </div>
        </div>

        <!-- Badge de estatus -->
        <div class="sug-estatus-wrap">
          <span
            class="sug-estatus-badge"
            :class="getEstatusLabel(sug.estatus).clase"
          >
            {{ getEstatusLabel(sug.estatus).texto }}
          </span>
        </div>
      </div>
    </div>

    <!-- Pie de panel -->
    <div class="suggestions-footer">
      ¿No encuentras al estudiante? Verifica en Servicios Escolares
    </div>
  </div>
</template>

<style scoped>
.suggestions-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 50;
  background: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 12px;
  box-shadow: 0 12px 28px -4px rgba(15, 37, 72, 0.22), 0 4px 10px -2px rgba(15, 37, 72, 0.1);
  margin-top: 6px;
  overflow: hidden;
  text-align: left;
}

.suggestions-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  border-bottom: 1px solid #F1F5F9;
  background: #F8FAFC;
}

.suggestions-count {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.8rem;
  font-weight: 600;
  color: #1E293B;
}

.dot-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #10B981;
  display: inline-block;
  flex-shrink: 0;
}

.suggestions-hint {
  font-size: 0.74rem;
  color: #94A3B8;
  font-style: italic;
}

.suggestions-list {
  max-height: 280px;
  overflow-y: auto;
}

.suggestion-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 11px 16px;
  cursor: pointer;
  border-bottom: 1px solid #F1F5F9;
  transition: background 0.15s ease;
}

.suggestion-card:last-child {
  border-bottom: none;
}

.suggestion-card:hover {
  background: #EBF3FF;
}

.sug-avatar {
  width: 38px;
  height: 38px;
  min-width: 38px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.82rem;
  font-weight: 700;
  color: #FFFFFF;
  letter-spacing: 0.03em;
}

.sug-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.sug-top-row {
  display: flex;
  align-items: baseline;
  gap: 10px;
}

.sug-matricula {
  font-weight: 700;
  font-size: 0.88rem;
  color: #1B396A;
  font-family: monospace;
  flex-shrink: 0;
}

.sug-nombre-completo {
  font-weight: 600;
  font-size: 0.87rem;
  color: #1E293B;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sug-bottom-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.76rem;
  color: #64748B;
}

.sug-carrera {
  font-weight: 500;
}

.sug-separador {
  color: #CBD5E1;
}

.sug-semestre {
  font-weight: 500;
}

.sug-estatus-wrap {
  flex-shrink: 0;
  margin-left: auto;
}

.sug-estatus-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  white-space: nowrap;
}

.badge-al-corriente {
  background: #ECFDF5;
  color: #065F46;
  border: 1px solid #A7F3D0;
}

.badge-adeudo {
  background: #FEF3C7;
  color: #92400E;
  border: 1px solid #FCD34D;
}

.badge-egresado {
  background: #EDE9FE;
  color: #5B21B6;
  border: 1px solid #C4B5FD;
}

.badge-default {
  background: #F1F5F9;
  color: #475569;
  border: 1px solid #CBD5E1;
}

.suggestions-footer {
  padding: 8px 16px;
  font-size: 0.74rem;
  color: #94A3B8;
  text-align: center;
  border-top: 1px solid #F1F5F9;
  background: #F8FAFC;
}

:global(.dark-theme) .suggestions-dropdown {
  background: #1E293B;
  border-color: #334155;
  box-shadow: 0 12px 28px -4px rgba(0, 0, 0, 0.45);
}

:global(.dark-theme) .suggestions-header,
:global(.dark-theme) .suggestions-footer {
  background: #0F172A;
  border-color: #334155;
}

:global(.dark-theme) .suggestions-count {
  color: #E2E8F0;
}

:global(.dark-theme) .suggestion-card {
  border-color: #334155;
}

:global(.dark-theme) .suggestion-card:hover {
  background: rgba(27, 57, 106, 0.35);
}

:global(.dark-theme) .sug-matricula {
  color: #93C5FD;
}

:global(.dark-theme) .sug-nombre-completo {
  color: #E2E8F0;
}

:global(.dark-theme) .sug-bottom-row {
  color: #94A3B8;
}

:global(.dark-theme) .badge-al-corriente {
  background: rgba(16, 185, 129, 0.15);
  color: #6EE7B7;
  border-color: rgba(16, 185, 129, 0.3);
}

:global(.dark-theme) .badge-adeudo {
  background: rgba(245, 158, 11, 0.15);
  color: #FCD34D;
  border-color: rgba(245, 158, 11, 0.3);
}

:global(.dark-theme) .badge-egresado {
  background: rgba(139, 92, 246, 0.15);
  color: #C4B5FD;
  border-color: rgba(139, 92, 246, 0.3);
}

:global(.dark-theme) .badge-default {
  background: rgba(100, 116, 139, 0.2);
  color: #CBD5E1;
  border-color: rgba(100, 116, 139, 0.3);
}

:global(.dark-theme) .suggestions-footer {
  color: #64748B;
}
</style>
