<template>
  <div class="switch-container">
    <label class="switch">
      <input type="checkbox" :checked="modelValue" @change="onChange" />
      <span class="slider"></span>
    </label>
    <span>{{ label }}</span>
  </div>
</template>

<script setup lang="ts">

const props = defineProps<{
  modelValue: boolean;
  label: string;
  onChange: (e: Event) => void;
}>();

const onChange = (event: Event) => {
  props.onChange(event);
};

</script>

<style scoped>

.switch-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.switch {
  position: relative;
  display: inline-block;
  width: 34px;
  height: 20px;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  border-radius: 20px;
  cursor: pointer;
  transition: 0.4s;
}

.slider:before {
  position: absolute;
  content: '';
  height: 14px;
  width: 14px;
  left: 3px;
  bottom: 3px;
  background-color: white;
  border-radius: 50%;
  transition: 0.4s;
}

input:checked + .slider {
  background-color: #2196F3;
}

input:checked + .slider:before {
  transform: translateX(14px);
}
</style>
