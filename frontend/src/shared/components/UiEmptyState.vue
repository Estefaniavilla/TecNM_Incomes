<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string
    description?: string
    variant?: 'receipt' | 'search' | 'cart'
  }>(),
  {
    description: '',
    variant: 'receipt',
  },
)
</script>

<template>
  <div class="ui-empty-state">
    <div class="ui-empty-icon-wrap">
      <!-- Receipt illustration -->
      <svg
        v-if="variant === 'receipt'"
        class="ui-empty-svg"
        viewBox="0 0 80 80"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <rect x="18" y="10" width="44" height="60" rx="8" class="empty-bg" stroke="currentColor" stroke-width="2.5" />
        <line x1="28" y1="24" x2="52" y2="24" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" />
        <line x1="28" y1="34" x2="46" y2="34" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" opacity="0.7" />
        <line x1="28" y1="44" x2="52" y2="44" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" opacity="0.7" />
        <circle cx="52" cy="52" r="14" class="empty-circle-bg" stroke="currentColor" stroke-width="2.5" />
        <path d="M48 52h8M52 48v8" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
      </svg>

      <!-- Cart / Concept illustration -->
      <svg
        v-else-if="variant === 'cart'"
        class="ui-empty-svg"
        viewBox="0 0 80 80"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <circle cx="40" cy="40" r="32" class="empty-bg" stroke="currentColor" stroke-width="2" stroke-dasharray="4 4" />
        <path d="M26 28h8l4 24h20l3-16H32" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />
        <circle cx="39" cy="58" r="2.5" fill="currentColor" />
        <circle cx="57" cy="58" r="2.5" fill="currentColor" />
      </svg>

      <!-- Search illustration -->
      <svg
        v-else
        class="ui-empty-svg"
        viewBox="0 0 80 80"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <circle cx="36" cy="36" r="18" stroke="currentColor" stroke-width="2.5" class="empty-bg" />
        <line x1="49" y1="49" x2="62" y2="62" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
      </svg>
    </div>

    <h3 class="ui-empty-title">{{ title }}</h3>
    <p v-if="description" class="ui-empty-desc">{{ description }}</p>
    <div v-if="$slots.default" class="ui-empty-actions">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.ui-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 32px 16px;
  color: var(--color-text-muted, #64748b);
}

.ui-empty-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 14px;
}

.ui-empty-svg {
  width: 64px;
  height: 64px;
  color: var(--color-brand, #3b82f6);
  opacity: 0.85;
}

.empty-bg {
  fill: rgba(59, 130, 246, 0.05);
}

.empty-circle-bg {
  fill: var(--color-surface, #ffffff);
}

.ui-empty-title {
  margin: 0 0 6px 0;
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text, #1e293b);
}

.ui-empty-desc {
  margin: 0;
  max-width: 320px;
  font-size: 13px;
  line-height: 1.45;
  color: var(--color-text-muted, #64748b);
}

.ui-empty-actions {
  margin-top: 16px;
}
</style>
