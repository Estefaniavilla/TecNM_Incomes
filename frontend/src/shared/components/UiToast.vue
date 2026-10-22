<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'

export type ToastTone = 'success' | 'error' | 'warning' | 'info'

interface ToastItem {
  id: number
  message: string
  tone: ToastTone
}

const toasts = ref<ToastItem[]>([])
let nextId = 1
const timers = new Map<number, ReturnType<typeof setTimeout>>()

function show(message: string, tone: ToastTone = 'success', duration = 4000) {
  const id = nextId++
  toasts.value.push({ id, message, tone })

  const timer = setTimeout(() => {
    dismiss(id)
  }, duration)
  timers.set(id, timer)
}

function dismiss(id: number) {
  const timer = timers.get(id)
  if (timer) {
    clearTimeout(timer)
    timers.delete(id)
  }
  toasts.value = toasts.value.filter(t => t.id !== id)
}

onBeforeUnmount(() => {
  timers.forEach(timer => clearTimeout(timer))
  timers.clear()
})

defineExpose({
  show,
  success: (msg: string, dur?: number) => show(msg, 'success', dur),
  error: (msg: string, dur?: number) => show(msg, 'error', dur),
  warning: (msg: string, dur?: number) => show(msg, 'warning', dur),
  info: (msg: string, dur?: number) => show(msg, 'info', dur),
  dismiss,
})
</script>

<template>
  <div class="ui-toast-container" aria-live="polite" role="region" aria-label="Notificaciones">
    <TransitionGroup name="toast-slide">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="ui-toast-card"
        :class="`toast-${toast.tone}`"
        role="alert"
      >
        <!-- Icon -->
        <div class="toast-icon">
          <!-- Success -->
          <svg
            v-if="toast.tone === 'success'"
            viewBox="0 0 20 20"
            fill="currentColor"
            class="toast-svg"
          >
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
          </svg>
          <!-- Error -->
          <svg
            v-else-if="toast.tone === 'error'"
            viewBox="0 0 20 20"
            fill="currentColor"
            class="toast-svg"
          >
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
          </svg>
          <!-- Warning -->
          <svg
            v-else-if="toast.tone === 'warning'"
            viewBox="0 0 20 20"
            fill="currentColor"
            class="toast-svg"
          >
            <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
          </svg>
          <!-- Info -->
          <svg
            v-else
            viewBox="0 0 20 20"
            fill="currentColor"
            class="toast-svg"
          >
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </div>

        <!-- Message -->
        <div class="toast-body">
          <span class="toast-message">{{ toast.message }}</span>
        </div>

        <!-- Close button -->
        <button
          type="button"
          class="toast-close"
          aria-label="Cerrar notificación"
          @click="dismiss(toast.id)"
        >
          &times;
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.ui-toast-container {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 420px;
  width: calc(100% - 48px);
  pointer-events: none;
}

.ui-toast-card {
  pointer-events: auto;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 500;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.15), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  border: 1px solid transparent;
}

.toast-success {
  background: #f0fdf4;
  color: #166534;
  border-color: #bbf7d0;
}

.toast-error {
  background: #fef2f2;
  color: #991b1b;
  border-color: #fecaca;
}

.toast-warning {
  background: #fffbeb;
  color: #92400e;
  border-color: #fde68a;
}

.toast-info {
  background: #eff6ff;
  color: #1e40af;
  border-color: #bfdbfe;
}

.toast-icon {
  flex-shrink: 0;
  display: flex;
  align-items: center;
}

.toast-svg {
  width: 20px;
  height: 20px;
}

.toast-body {
  flex: 1;
  line-height: 1.4;
  word-break: break-word;
}

.toast-close {
  background: none;
  border: none;
  font-size: 18px;
  line-height: 1;
  color: currentColor;
  opacity: 0.6;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 4px;
  transition: opacity 0.15s;
}

.toast-close:hover {
  opacity: 1;
}

/* Animations */
.toast-slide-enter-active {
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-slide-leave-active {
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-slide-enter-from {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}

.toast-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px) scale(0.9);
}

@media (max-width: 640px) {
  .ui-toast-container {
    top: auto;
    bottom: 20px;
    right: 16px;
    left: 16px;
    width: auto;
    max-width: none;
  }
}
</style>
