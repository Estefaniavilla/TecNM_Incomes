<script setup lang="ts">
import { useRouter } from 'vue-router'
import ModalEmpleados from '../components/ModalEmpleados.vue'

const router = useRouter()

function volverACaja(): void {
  router.push('/')
}
</script>

<template>
  <div class="view-wrapper">
    <ModalEmpleados @close="volverACaja" />
  </div>
</template>

<style scoped>
.view-wrapper {
  min-height: 100vh;
  background-color: var(--color-bg-main, #F4F6F9);
}
</style>
