<script setup lang="ts">
import { RouterView } from 'vue-router'
import { UiShell } from '../../shared/components'
</script>

<template>
  <UiShell :show-header="false" :show-sidebar="false">
    <RouterView />
    <template #footer>
      <span>Tecnológico Nacional de México</span>
      <span>Campus Monclova · Sistema de Control de Caja</span>
    </template>
  </UiShell>
</template>
