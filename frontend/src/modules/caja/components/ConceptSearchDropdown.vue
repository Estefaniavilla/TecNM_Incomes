<script setup lang="ts">
import type { ConceptoCatalogo } from '../types/caja'

withDefaults(
  defineProps<{
    sugerencias: ConceptoCatalogo[]
    visible?: boolean
  }>(),
  {
    visible: true,
  },
)

const emit = defineEmits<{
  select: [concepto: ConceptoCatalogo]
}>()
</script>

<template>
  <div v-if="sugerencias.length > 0 && visible" class="concept-dropdown">
    <div class="concept-header">
      <span class="concept-count">
        <span class="dot-indicator"></span>
        Conceptos encontrados ( {{ sugerencias.length }} )
      </span>
      <span class="concept-hint">Presiona Enter o haz click para seleccionar</span>
    </div>

    <div class="concept-list">
      <div
        v-for="c in sugerencias"
        :key="c.id || c.clave"
        class="concept-card"
        @mousedown.prevent="emit('select', c)"
      >
        <div class="concept-left">
          <span class="concept-clave">{{ c.clave }}</span>
          <div class="concept-text">
            <span class="concept-desc">{{ c.descripcion }}</span>
            <span v-if="c.categoria" class="concept-cat">{{ c.categoria }}</span>
          </div>
        </div>

        <div class="concept-right">
          <span v-if="c.aplicaBeca" class="badge-beca">Beca</span>
          <span class="concept-cuota">${{ (c.cuotaBase ?? 0).toFixed(2) }}</span>
        </div>
      </div>
    </div>

    <div class="concept-footer">
      Catálogo oficial de cuotas y conceptos de cobro
    </div>
  </div>
</template>

<style scoped>
.concept-dropdown {
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

.concept-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  border-bottom: 1px solid #F1F5F9;
  background: #F8FAFC;
}

.concept-count {
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
  background: #C5A059;
  display: inline-block;
  flex-shrink: 0;
}

.concept-hint {
  font-size: 0.74rem;
  color: #94A3B8;
  font-style: italic;
}

.concept-list {
  max-height: 280px;
  overflow-y: auto;
}

.concept-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 16px;
  cursor: pointer;
  border-bottom: 1px solid #F1F5F9;
  transition: background 0.15s ease;
}

.concept-card:last-child {
  border-bottom: none;
}

.concept-card:hover {
  background: #EBF3FF;
}

.concept-left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.concept-clave {
  font-weight: 700;
  font-size: 0.8rem;
  font-family: monospace;
  color: #1B396A;
  background: #EBF3FF;
  padding: 3px 8px;
  border-radius: 4px;
  flex-shrink: 0;
}

.concept-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.concept-desc {
  font-weight: 600;
  font-size: 0.87rem;
  color: #1E293B;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.concept-cat {
  font-size: 0.74rem;
  color: #64748B;
}

.concept-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.badge-beca {
  background: #EDE9FE;
  color: #6D28D9;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
}

.concept-cuota {
  font-weight: 700;
  font-size: 0.88rem;
  color: #1E293B;
  font-family: monospace;
}

.concept-footer {
  padding: 8px 16px;
  font-size: 0.74rem;
  color: #94A3B8;
  text-align: center;
  border-top: 1px solid #F1F5F9;
  background: #F8FAFC;
}

:global(.dark-theme) .concept-dropdown {
  background: #1E293B;
  border-color: #334155;
  box-shadow: 0 12px 28px -4px rgba(0, 0, 0, 0.45);
}

:global(.dark-theme) .concept-header,
:global(.dark-theme) .concept-footer {
  background: #0F172A;
  border-color: #334155;
}

:global(.dark-theme) .concept-count {
  color: #E2E8F0;
}

:global(.dark-theme) .concept-card {
  border-color: #334155;
}

:global(.dark-theme) .concept-card:hover {
  background: rgba(27, 57, 106, 0.35);
}

:global(.dark-theme) .concept-clave {
  background: rgba(59, 130, 246, 0.15);
  color: #93C5FD;
}

:global(.dark-theme) .concept-desc {
  color: #E2E8F0;
}

:global(.dark-theme) .concept-cuota {
  color: #E2E8F0;
}

:global(.dark-theme) .badge-beca {
  background: rgba(139, 92, 246, 0.2);
  color: #C4B5FD;
}

:global(.dark-theme) .concept-footer {
  color: #64748B;
}
</style>
