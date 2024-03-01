<script setup lang="ts">
import { PropType } from 'vue';
import { WindowActivityEntity } from '../../electron/main/entities/WindowActivityEntity';

const props = defineProps({
  data: {
    type: Object as PropType<WindowActivityEntity[]>,
    default: null,
    required: false
  },
  shouldObfuscate: {
    type: Boolean,
    default: false,
    required: false
  },
  defaultValue: {
    type: String,
    required: true
  }
});
import { ref, defineEmits } from 'vue';
import { DataExportType } from '../../shared/DataExportType.enum';

const emits = defineEmits(['change']);

const selectedOption = ref<string>(props.defaultValue);

const emitChange = async () => {
  emits('change', selectedOption.value);
};
</script>
<template>
  <div class="my-5 border border-slate-400 p-2">
    <div class="prose">
      <h2>Decide how your Window Activity data is shared</h2>
      <p v-if="selectedOption != DataExportType.None">
        Here is a sample of your {{ shouldObfuscate ? 'modified' : 'unmodified' }} data:
      </p>
    </div>
    <div v-if="selectedOption != DataExportType.None" class="max-h-48 overflow-auto">
      <table
        class="table table-zebra table-pin-rows w-full overflow-auto text-xs"
        style="width: 1500px"
      >
        <thead class="border-b">
          <tr>
            <th>Window Title</th>
            <th>URL</th>
            <th>Activity</th>
            <th>Process Name</th>
            <th>Process ID</th>
            <th>Timestamp</th>
          </tr>
        </thead>
        <tbody class="">
          <tr v-for="windowActivity in data" :key="windowActivity.id">
            <td>{{ windowActivity.windowTitle }}</td>
            <td>{{ windowActivity.url }}</td>
            <td>{{ windowActivity.activity }}</td>
            <td>{{ windowActivity.processName }}</td>
            <td>{{ windowActivity.processId }}</td>
            <td>{{ windowActivity.ts }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="mt-4 flex">
      <div class="mr-4">
        <div class="prose">
          <h2>How do you want to share your data?</h2>
        </div>
        <div class="form-control">
          <label class="label cursor-pointer">
            <span class="label-text">Share data as-is</span>
            <input
              v-model="selectedOption"
              type="radio"
              :value="DataExportType.All"
              class="radio checked:bg-blue-500"
              @change="emitChange"
            />
          </label>
        </div>
        <div class="form-control">
          <label class="label cursor-pointer">
            <span class="label-text">Obfuscate potentially sensitive data</span>
            <input
              v-model="selectedOption"
              type="radio"
              :value="DataExportType.Obfuscate"
              class="radio checked:bg-blue-500"
              @change="emitChange"
            />
          </label>
        </div>
        <div class="form-control">
          <label class="label cursor-pointer">
            <span class="label-text">Do not share this data</span>
            <input
              v-model="selectedOption"
              type="radio"
              :value="DataExportType.None"
              class="radio checked:bg-blue-500"
              @change="emitChange"
            />
          </label>
        </div>
      </div>
    </div>
  </div>
</template>
