<script setup lang="ts">
import { ref, defineEmits, PropType } from 'vue';
import { DataExportType } from '../../shared/DataExportType.enum';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';

const props = defineProps({
  data: {
    type: Object as PropType<WindowActivityDto[]>,
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

const emits = defineEmits(['optionChanged', 'obfuscationTermsChanged', 'obfuscateSampleData']);

const selectedOption = ref<string>(props.defaultValue);
const obfuscationTermsInput = ref<string>('');

const emitOptionChanged = async () => {
  emits('optionChanged', selectedOption.value);
};

const emitObfuscationTermsChanged = async () => {
  emits('obfuscationTermsChanged', obfuscationTermsInput.value);
};

const emitObfuscateSampleData = async () => {
  emits('obfuscateSampleData');
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
      <table class="table table-zebra table-pin-rows w-full overflow-auto text-xs">
        <thead class="border-b">
          <tr>
            <th>Window Title</th>
            <th>URL</th>
            <th>Activity</th>
            <th>Process Name</th>
            <th>Process Path</th>
            <th>Process ID</th>
            <th>Timestamp</th>
          </tr>
        </thead>
        <tbody class="">
          <tr v-for="windowActivity in data" :key="windowActivity.id">
            <td>
              <div class="max-w-56 truncate">{{ windowActivity.windowTitle }}</div>
            </td>
            <td>
              <div class="max-w-56 truncate">{{ windowActivity.url }}</div>
            </td>
            <td>{{ windowActivity.activity }}</td>
            <td>{{ windowActivity.processName }}</td>
            <td>
              <div class="max-w-56 truncate">{{ windowActivity.processPath }}</div>
            </td>
            <td>{{ windowActivity.processId }}</td>
            <td>{{ windowActivity.ts.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-8 flex flex-col">
      <div class="flex w-2/3 flex-col">
        <div class="prose">
          <h2>How do you want to share your data?</h2>
        </div>
        <div class="form-control">
          <label class="label cursor-pointer">
            <span class="label-text">{{
              obfuscationTermsInput.length > 0
                ? 'Share partially obfuscated data'
                : 'Share data as-is'
            }}</span>
            <input
              v-model="selectedOption"
              type="radio"
              :value="DataExportType.All"
              class="radio checked:bg-blue-500"
              @change="emitOptionChanged"
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
              @change="emitOptionChanged"
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
              @change="emitOptionChanged"
            />
          </label>
        </div>
        <div class="form-control">
          <label class="label cursor-pointer">
            <span class="label-text"
              >Only obfuscate data with the following (comma-separated) list of terms</span
            >
            <input
              v-model="selectedOption"
              type="radio"
              :value="DataExportType.ObfuscateWithTerms"
              class="radio checked:bg-blue-500"
              @change="emitOptionChanged"
            />
          </label>
        </div>
      </div>
      <div v-if="selectedOption === DataExportType.ObfuscateWithTerms">
        <div class="prose">
          <h3>Obfuscate content by terms</h3>
        </div>
        <div class="flex flex-col">
          <label class="form-control w-full">
            <div class="label">
              <span class="label-text">You can enter multiple terms separated by commas</span>
            </div>
            <input
              v-model="obfuscationTermsInput"
              type="text"
              placeholder="Enter, terms, here, ..."
              class="input input-bordered w-full text-sm"
              @input="emitObfuscationTermsChanged"
              @keyup.enter="emitObfuscateSampleData"
            />
          </label>
          <button class="btn btn-neutral ml-auto mt-2" @click="emitObfuscateSampleData">
            Obfuscate sample data
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
