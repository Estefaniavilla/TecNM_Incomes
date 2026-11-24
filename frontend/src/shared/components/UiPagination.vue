<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  currentPage: number
  totalItems: number
  pageSize?: number
  compact?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  pageSize: 5,
  compact: false,
})

const emit = defineEmits<{
  (e: 'update:currentPage', page: number): void
  (e: 'change', page: number): void
}>()

const totalPages = computed(() => {
  if (props.totalItems <= 0) return 1
  return Math.max(1, Math.ceil(props.totalItems / props.pageSize))
})

const startItem = computed(() => {
  if (props.totalItems === 0) return 0
  return (props.currentPage - 1) * props.pageSize + 1
})

const endItem = computed(() => {
  return Math.min(props.currentPage * props.pageSize, props.totalItems)
})

const pagesToShow = computed(() => {
  const current = props.currentPage
  const total = totalPages.value
  if (total <= 5) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }
  if (current <= 3) {
    return [1, 2, 3, 4, 5]
  }
  if (current >= total - 2) {
    return [total - 4, total - 3, total - 2, total - 1, total]
  }
  return [current - 2, current - 1, current, current + 1, current + 2]
})

function irAPagina(p: number): void {
  if (p < 1 || p > totalPages.value || p === props.currentPage) return
  emit('update:currentPage', p)
  emit('change', p)
}
</script>

<template>
  <div class="ui-pagination-container" :class="{ 'is-compact': compact }">
    <div class="pagination-info">
      <span v-if="totalItems > 0">
        Mostrando <strong class="info-highlight">{{ startItem }}</strong> - <strong class="info-highlight">{{ endItem }}</strong> de <strong class="info-highlight">{{ totalItems }}</strong> registros
      </span>
      <span v-else class="text-muted">Sin registros</span>
    </div>

    <div v-if="totalPages > 1" class="pagination-controls">
      <!-- Botón Primera Página -->
      <button
        type="button"
        class="page-btn page-nav-btn"
        :disabled="currentPage === 1"
        title="Primera página"
        aria-label="Primera página"
        @click="irAPagina(1)"
      >
        <span aria-hidden="true">&laquo;</span>
      </button>

      <!-- Botón Anterior -->
      <button
        type="button"
        class="page-btn page-nav-btn"
        :disabled="currentPage === 1"
        title="Página anterior"
        aria-label="Página anterior"
        @click="irAPagina(currentPage - 1)"
      >
        <span aria-hidden="true">&lsaquo;</span>
      </button>

      <!-- Números de Página -->
      <button
        v-for="p in pagesToShow"
        :key="p"
        type="button"
        class="page-btn page-number-btn"
        :class="{ active: p === currentPage }"
        :title="`Página ${p}`"
        @click="irAPagina(p)"
      >
        {{ p }}
      </button>

      <!-- Botón Siguiente -->
      <button
        type="button"
        class="page-btn page-nav-btn"
        :disabled="currentPage === totalPages"
        title="Página siguiente"
        aria-label="Página siguiente"
        @click="irAPagina(currentPage + 1)"
      >
        <span aria-hidden="true">&rsaquo;</span>
      </button>

      <!-- Botón Última Página -->
      <button
        type="button"
        class="page-btn page-nav-btn"
        :disabled="currentPage === totalPages"
        title="Última página"
        aria-label="Última página"
        @click="irAPagina(totalPages)"
      >
        <span aria-hidden="true">&raquo;</span>
      </button>
    </div>
    <div v-else class="pagination-single-page">
      <span class="single-page-badge">Página 1 de 1</span>
    </div>
  </div>
</template>

<style scoped>
.ui-pagination-container {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 14px;
  background: #f8fafc;
  border-top: 1px solid #cbd5e1;
  font-size: 0.82rem;
  color: #334155;
  user-select: none;
  min-height: 42px;
  box-sizing: border-box;
}

.pagination-info {
  display: flex;
  align-items: center;
  gap: 4px;
}

.info-highlight {
  font-weight: 700;
  color: #1e293b;
}

.pagination-controls {
  display: flex;
  align-items: center;
  gap: 4px;
}

.page-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
  height: 28px;
  padding: 0 6px;
  border: 1px solid #cbd5e1;
  border-radius: 4px;
  background-color: #ffffff;
  color: #1e293b;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease-in-out;
}

.page-btn:hover:not(:disabled) {
  background-color: #1b396a;
  color: #ffffff;
  border-color: #1b396a;
}

.page-btn.active {
  background-color: #1b396a;
  color: #ffffff;
  border-color: #1b396a;
  box-shadow: 0 1px 3px rgba(27, 57, 106, 0.3);
}

.page-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background-color: #f1f5f9;
  border-color: #e2e8f0;
}

.single-page-badge {
  font-size: 0.75rem;
  padding: 3px 8px;
  border-radius: 4px;
  background-color: #e2e8f0;
  color: #475569;
  font-weight: 600;
}

/* Modo compacto */
.ui-pagination-container.is-compact {
  padding: 4px 8px;
  min-height: 34px;
  font-size: 0.75rem;
}

.ui-pagination-container.is-compact .page-btn {
  min-width: 24px;
  height: 24px;
  font-size: 0.75rem;
}

/* Modo Oscuro Institucional */
:global([data-theme="dark"]) .ui-pagination-container,
:global(.dark-theme) .ui-pagination-container {
  background: #0c1a30;
  border-top-color: #1b3762;
  color: #94a3b8;
}

:global([data-theme="dark"]) .info-highlight,
:global(.dark-theme) .info-highlight {
  color: #f8fafc;
}

:global([data-theme="dark"]) .page-btn,
:global(.dark-theme) .page-btn {
  background-color: #10223d;
  border-color: #1f3e6e;
  color: #cbd5e1;
}

:global([data-theme="dark"]) .page-btn:hover:not(:disabled),
:global(.dark-theme) .page-btn:hover:not(:disabled) {
  background-color: #244b88;
  border-color: #3b82f6;
  color: #ffffff;
}

:global([data-theme="dark"]) .page-btn.active,
:global(.dark-theme) .page-btn.active {
  background-color: #1b396a;
  border-color: #d4b06a;
  color: #ffffff;
}

:global([data-theme="dark"]) .page-btn:disabled,
:global(.dark-theme) .page-btn:disabled {
  background-color: #081220;
  border-color: #132742;
  color: #475569;
}

:global([data-theme="dark"]) .single-page-badge,
:global(.dark-theme) .single-page-badge {
  background-color: #10223d;
  border: 1px solid #1b3762;
  color: #94a3b8;
}
</style>
