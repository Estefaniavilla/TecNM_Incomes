<script setup lang="ts">
import { computed, nextTick, onMounted, ref, useAttrs, useId, watch } from 'vue'

defineOptions({ inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    title: string
    description?: string
    open?: boolean
  }>(),
  {
    open: false,
    description: undefined,
  },
)

const emit = defineEmits<{
  (e: 'close'): void
}>()

const model = defineModel<boolean>({ default: false })
const isOpen = computed(() => Boolean(model.value || props.open))
const attrs = useAttrs()
const dialog = ref<HTMLDialogElement | null>(null)
const titleId = useId()
let previouslyFocused: HTMLElement | null = null

async function openDialog(): Promise<void> {
  await nextTick()
  if (!dialog.value || dialog.value.open) return

  previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null
  try {
    dialog.value.showModal()
  } catch {
    // Fallback if browser doesn't support showModal or already open
    if (dialog.value) dialog.value.open = true
  }
}

function restoreFocus(): void {
  const target = previouslyFocused
  previouslyFocused = null
  target?.focus()
}

function closeDialog(): void {
  if (dialog.value && dialog.value.open) {
    try {
      dialog.value.close()
    } catch {
      dialog.value.open = false
    }
  }
  restoreFocus()
  emit('close')
}

function syncModelFromDialog(): void {
  model.value = false
  closeDialog()
}

function syncDialog(): void {
  if (isOpen.value) {
    void openDialog()
  } else {
    closeDialog()
  }
}

watch(isOpen, syncDialog)
onMounted(syncDialog)
</script>

<template>
  <dialog
    v-if="isOpen"
    ref="dialog"
    v-bind="attrs"
    :aria-labelledby="titleId"
    @cancel="syncModelFromDialog"
    @close="syncModelFromDialog"
  >
    <div class="ui-dialog-header">
      <h2 :id="titleId">{{ title }}</h2>
      <p v-if="description" class="ui-dialog-desc">{{ description }}</p>
    </div>
    <div class="ui-dialog-content"><slot /></div>
    <div v-if="$slots.actions" class="row"><slot name="actions" /></div>
  </dialog>
</template>
