<script setup lang="ts">
withDefaults(
  defineProps<{
    size?: 'sm' | 'md' | 'lg'
  }>(),
  {
    size: 'md',
  },
)
</script>

<template>
  <svg
    class="ui-spinner"
    :class="`ui-spinner--${size}`"
    viewBox="0 0 24 24"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    role="status"
    aria-label="Cargando..."
  >
    <circle
      class="ui-spinner-track"
      cx="12"
      cy="12"
      r="10"
      stroke="currentColor"
      stroke-width="3"
    />
    <path
      class="ui-spinner-head"
      d="M12 2a10 10 0 0 1 10 10"
      stroke="currentColor"
      stroke-width="3"
      stroke-linecap="round"
    />
  </svg>
</template>

<style scoped>
.ui-spinner {
  display: inline-block;
  animation: ui-spin 0.75s linear infinite;
  vertical-align: middle;
  flex-shrink: 0;
}
.ui-spinner--sm {
  width: 16px;
  height: 16px;
}
.ui-spinner--md {
  width: 22px;
  height: 22px;
}
.ui-spinner--lg {
  width: 32px;
  height: 32px;
}
.ui-spinner-track {
  opacity: 0.25;
}
.ui-spinner-head {
  opacity: 1;
}
@keyframes ui-spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
