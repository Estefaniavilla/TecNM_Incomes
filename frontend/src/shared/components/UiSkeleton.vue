<script setup lang="ts">
withDefaults(
  defineProps<{
    width?: string
    height?: string
    rounded?: string
    lines?: number
  }>(),
  {
    width: '100%',
    height: '1rem',
    rounded: '6px',
    lines: 1,
  },
)
</script>

<template>
  <div class="ui-skeleton-wrapper">
    <div
      v-for="i in lines"
      :key="i"
      class="ui-skeleton"
      :style="{
        width: lines > 1 && i === lines ? '70%' : width,
        height,
        borderRadius: rounded,
      }"
      aria-hidden="true"
    />
  </div>
</template>

<style scoped>
.ui-skeleton-wrapper {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}

.ui-skeleton {
  background: linear-gradient(
    90deg,
    rgba(128, 128, 128, 0.12) 25%,
    rgba(128, 128, 128, 0.24) 37%,
    rgba(128, 128, 128, 0.12) 63%
  );
  background-size: 400% 100%;
  animation: ui-skeleton-shimmer 1.4s ease infinite;
}

@keyframes ui-skeleton-shimmer {
  0% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0 50%;
  }
}
</style>
