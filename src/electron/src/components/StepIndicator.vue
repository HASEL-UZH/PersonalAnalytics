<script setup lang="ts">
defineProps({
  totalSteps: {
    type: Number,
    required: true
  },
  currentStep: {
    type: Number,
    required: true
  }
});
</script>
<template>
  <div class="flex flex-row justify-between">
    <template v-for="i in [...Array(totalSteps).keys()]" :key="i">
      <div
        class="ml-1 h-3 w-3 rounded-full bg-neutral-300 transition-all duration-300 dark:bg-neutral-700"
        :class="{
          'bg-neutral-700 dark:bg-white': i === currentStep
        }"
      />
    </template>
  </div>
</template>
