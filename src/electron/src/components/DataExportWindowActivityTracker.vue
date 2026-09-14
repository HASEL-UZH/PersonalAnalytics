<script setup lang="ts">
import { computed, ref, PropType } from 'vue';
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

// URLs are only collected for supported browsers, so hide the column if the sample has none
const hasUrls = computed(() => props.data?.some((d) => d.url && d.url.length > 0) ?? false);

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
    <div class="prose max-w-none">
      <h2>How do you want to share your Window Activity data?</h2>
      <p>
        This records which application or window was in focus at which time, along with the program
        name, a timestamp, and an activity category that PersonalAnalytics derives locally from the
        window (for example coding, email or browsing). Window titles may occasionally contain
        sensitive content, such as document and project names, so you can choose to obfuscate them
        below. The sample updates to show exactly what the researchers would receive.
      </p>
    </div>
    <div class="mt-4 flex w-full flex-col">
      <div class="form-control">
        <label class="label flex cursor-pointer items-center justify-start">
          <input
            v-model="selectedOption"
            type="radio"
            :value="DataExportType.All"
            class="radio checked:bg-blue-500"
            @change="emitOptionChanged"
          />
          <span class="label-text ml-2"><b>Share data as-is</b></span>
        </label>
      </div>
      <div class="form-control">
        <label class="label flex cursor-pointer items-center justify-start">
          <input
            v-model="selectedOption"
            type="radio"
            :value="DataExportType.Obfuscate"
            class="radio checked:bg-blue-500"
            @change="emitOptionChanged"
          />
          <span class="label-text ml-2"
            ><b>Obfuscate potentially sensitive data</b>
            <span class="opacity-70">&nbsp;(each title becomes a random placeholder)</span></span
          >
        </label>
      </div>
      <div class="form-control">
        <label class="label flex cursor-pointer items-center justify-start">
          <input
            v-model="selectedOption"
            type="radio"
            :value="DataExportType.ObfuscateWithTerms"
            class="radio checked:bg-blue-500"
            @change="emitOptionChanged"
          />
          <span class="label-text ml-2"
            ><b>Only obfuscate data with the following (comma-separated) list of terms</b>
            <span class="opacity-70"
              >&nbsp;(entries matching your terms become [anonymized], the rest stays as-is)</span
            ></span
          >
        </label>
      </div>
      <div v-if="selectedOption === DataExportType.ObfuscateWithTerms">
        <div class="mb-2 mt-1 flex justify-center">
          <label class="form-control w-full">
            <input
              v-model.trim="obfuscationTermsInput"
              type="text"
              placeholder="Enter, terms, here, ..."
              class="input input-bordered w-full text-sm"
              @input="emitObfuscationTermsChanged"
              @keyup.enter="emitObfuscateSampleData"
            />
          </label>
          <button class="btn btn-neutral ml-2" @click="emitObfuscateSampleData">Obfuscate</button>
        </div>
      </div>
      <div class="form-control">
        <label class="label flex cursor-pointer items-center justify-start">
          <input
            v-model="selectedOption"
            type="radio"
            :value="DataExportType.None"
            class="radio checked:bg-blue-500"
            @change="emitOptionChanged"
          />
          <span class="label-text ml-2"><b>Do not share this data</b></span>
        </label>
      </div>
    </div>
    <div
      class="relative mt-5"
      :class="{
        'cursor-not-allowed overflow-hidden opacity-50': selectedOption === DataExportType.None
      }"
    >
      <div
        v-if="selectedOption === DataExportType.None"
        class="absolute inset-0 z-10 flex items-center justify-center bg-slate-800 bg-opacity-40"
      >
        <p class="bg-slate-800 p-5 text-lg text-white">This data is not being shared</p>
      </div>
      <div class="max-h-48 w-full overflow-y-auto">
        <table class="table table-zebra table-pin-rows max-h-48 w-full text-xs">
          <thead class="border-b">
            <tr>
              <th>Window Title</th>
              <th v-if="hasUrls">URL</th>
              <th>Activity</th>
              <th>Process Name</th>
              <th>Process Path</th>
              <th>Process ID</th>
              <th>Timestamp</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="windowActivity in data" :key="windowActivity.id">
              <td>
                <div class="max-w-56 truncate">{{ windowActivity.windowTitle }}</div>
              </td>
              <td v-if="hasUrls">
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
    </div>
  </div>
</template>
