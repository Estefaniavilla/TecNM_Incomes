<script setup lang="ts">
import type { ReciboCaja } from '../types/caja'

withDefaults(
  defineProps<{
    sugerencias: ReciboCaja[]
    visible?: boolean
  }>(),
  {
    visible: true,
  },
)

const emit = defineEmits<{
  select: [recibo: ReciboCaja]
}>()

function getBadgeClass(estado: string): string {
  const e = (estado || '').toUpperCase()
  if (e === 'REGISTRADO') return 'badge-registrado'
  if (e === 'CANCELADO') return 'badge-cancelado'
  return 'badge-previo'
}
</script>

<template>
  <div v-if="sugerencias.length > 0 && visible" class="receipt-dropdown">
    <!-- Encabezado de sugerencias -->
    <div class="receipt-header">
      <span class="receipt-count">
        <span class="dot-indicator"></span>
        Recibos encontrados ( {{ sugerencias.length }} )
      </span>
      <span class="receipt-hint">Presiona Enter o haz click para cargar</span>
    </div>

    <!-- Lista de tarjetas de recibo -->
    <div class="receipt-list">
      <div
        v-for="rec in sugerencias"
        :key="rec.id || rec.numeroRecibo"
        class="receipt-card"
        @mousedown.prevent="emit('select', rec)"
      >
        <div class="receipt-badge-col">
          <span class="rec-number-pill">#{{ rec.numeroRecibo }}</span>
        </div>

        <div class="receipt-info-col">
          <div class="rec-top-row">
            <span class="rec-student-name">{{ rec.alumno?.nombreCompleto || 'Sin nombre' }}</span>
            <span v-if="rec.alumno?.matricula" class="rec-student-mat">({{ rec.alumno.matricula }})</span>
          </div>
          <div class="rec-bottom-row">
            <span class="rec-folio">{{ rec.folioAdministrativo || `FOL-${rec.numeroRecibo}` }}</span>
            <span class="rec-sep">•</span>
            <span class="rec-date">{{ rec.fechaOperacion }}</span>
            <span v-if="rec.metodoPago" class="rec-metodo">• {{ rec.metodoPago }}</span>
          </div>
        </div>

        <div class="receipt-meta-col">
          <span class="rec-total">${{ (rec.total || 0).toFixed(2) }}</span>
          <span class="rec-status-badge" :class="getBadgeClass(rec.estado)">
            {{ rec.estado }}
          </span>
        </div>
      </div>
    </div>

    <div class="receipt-footer">
      Selecciona un recibo para consultar o gestionar su estado administrativo
    </div>
  </div>
</template>

<style scoped>
.receipt-dropdown {
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

.receipt-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  border-bottom: 1px solid #F1F5F9;
  background: #F8FAFC;
}

.receipt-count {
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
  background: #3B82F6;
  display: inline-block;
  flex-shrink: 0;
}

.receipt-hint {
  font-size: 0.74rem;
  color: #94A3B8;
  font-style: italic;
}

.receipt-list {
  max-height: 280px;
  overflow-y: auto;
}

.receipt-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 16px;
  cursor: pointer;
  border-bottom: 1px solid #F1F5F9;
  transition: background 0.15s ease;
}

.receipt-card:last-child {
  border-bottom: none;
}

.receipt-card:hover {
  background: #EBF3FF;
}

.receipt-badge-col {
  flex-shrink: 0;
}

.rec-number-pill {
  display: inline-block;
  background: #1B396A;
  color: #FFFFFF;
  font-size: 0.78rem;
  font-weight: 700;
  font-family: monospace;
  padding: 4px 8px;
  border-radius: 6px;
}

.receipt-info-col {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.rec-top-row {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.rec-student-name {
  font-weight: 600;
  font-size: 0.87rem;
  color: #1E293B;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rec-student-mat {
  font-size: 0.8rem;
  font-family: monospace;
  color: #64748B;
  flex-shrink: 0;
}

.rec-bottom-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.76rem;
  color: #64748B;
}

.rec-folio {
  font-family: monospace;
  color: #1B396A;
  font-weight: 500;
}

.rec-sep {
  color: #CBD5E1;
}

.rec-date {
  color: #64748B;
}

.rec-metodo {
  color: #94A3B8;
  text-transform: capitalize;
}

.receipt-meta-col {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}

.rec-total {
  font-weight: 700;
  font-size: 0.88rem;
  color: #1E293B;
}

.rec-status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  white-space: nowrap;
}

.badge-registrado {
  background: #ECFDF5;
  color: #065F46;
  border: 1px solid #A7F3D0;
}

.badge-cancelado {
  background: #FEF2F2;
  color: #991B1B;
  border: 1px solid #FECACA;
}

.badge-previo {
  background: #FFFBEB;
  color: #92400E;
  border: 1px solid #FDE68A;
}

.receipt-footer {
  padding: 8px 16px;
  font-size: 0.74rem;
  color: #94A3B8;
  text-align: center;
  border-top: 1px solid #F1F5F9;
  background: #F8FAFC;
}

:global(.dark-theme) .receipt-dropdown {
  background: #1E293B;
  border-color: #334155;
  box-shadow: 0 12px 28px -4px rgba(0, 0, 0, 0.45);
}

:global(.dark-theme) .receipt-header,
:global(.dark-theme) .receipt-footer {
  background: #0F172A;
  border-color: #334155;
}

:global(.dark-theme) .receipt-count {
  color: #E2E8F0;
}

:global(.dark-theme) .receipt-card {
  border-color: #334155;
}

:global(.dark-theme) .receipt-card:hover {
  background: rgba(27, 57, 106, 0.35);
}

:global(.dark-theme) .rec-student-name {
  color: #E2E8F0;
}

:global(.dark-theme) .rec-student-mat {
  color: #94A3B8;
}

:global(.dark-theme) .rec-folio {
  color: #93C5FD;
}

:global(.dark-theme) .rec-total {
  color: #E2E8F0;
}

:global(.dark-theme) .badge-registrado {
  background: rgba(16, 185, 129, 0.15);
  color: #6EE7B7;
  border-color: rgba(16, 185, 129, 0.3);
}

:global(.dark-theme) .badge-cancelado {
  background: rgba(239, 68, 68, 0.15);
  color: #FCA5A5;
  border-color: rgba(239, 68, 68, 0.3);
}

:global(.dark-theme) .badge-previo {
  background: rgba(245, 158, 11, 0.15);
  color: #FCD34D;
  border-color: rgba(245, 158, 11, 0.3);
}

:global(.dark-theme) .receipt-footer {
  color: #64748B;
}
</style>
