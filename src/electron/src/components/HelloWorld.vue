<script setup lang="ts">
import { ref } from 'vue';

defineProps({
  msg: { type: String, required: true }
});

const count = ref(0);
</script>

<template>
  <h1>{{ msg }}</h1>
  <h1 class="text-3xl font-bold underline">Hello world!</h1>

  <div class="card">
    <button type="button" class="btn" @click="count++">count is {{ count }}</button>
    <button class="bg-sky-700 px-4 py-2 text-white hover:bg-sky-800 sm:px-8 sm:py-3">...</button>
    <p>
      Edit
      <code>components/HelloWorld.vue</code> to test HMR
    </p>
  </div>
</template>

<style scoped lang="less"></style>
