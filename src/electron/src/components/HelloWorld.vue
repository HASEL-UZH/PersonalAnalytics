<script setup lang="ts">
import { ref } from 'vue';
import log from 'electron-log/renderer';

defineProps({
  msg: { type: String, required: true }
});

const count = ref(0);

log.info('Hello World from renderer process!');
</script>

<template>
  <h1>{{ msg }}</h1>
  <h1 class="text-3xl font-bold underline">Hello world!</h1>

  <div class="card">
    <button type="button" class="btn" @click="count++">count is {{ count }}</button>
    <p>
      Edit
      <code>components/HelloWorld.vue</code> to test HMR
    </p>
  </div>
</template>

<style scoped lang="less"></style>
