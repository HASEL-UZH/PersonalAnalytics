<script setup lang="ts">
import { ref, PropType } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import { DataExportType } from '../../shared/DataExportType.enum';
import UserInputDto from '../../shared/dto/UserInputDto';

const props = defineProps({
  studyInfo: {
    type: Object as PropType<StudyInfoDto>,
    default: null,
    required: false
  },
  data: {
    type: Object as PropType<UserInputDto[]>,
    default: null,
    required: false
  },
  defaultValue: {
    type: String,
    required: true
  }
});

const emits = defineEmits(['change']);

const selectedOption = ref<string>(props.defaultValue);

const emitChange = () => {
  emits('change', selectedOption.value);
};
</script>
<template>
  <div class="my-5 border border-slate-400 p-2">
    <div class="prose">
      <h2>Decide how your User Input data is shared</h2>
      <p v-if="selectedOption != DataExportType.None">Here is a sample of your unmodified data:</p>
    </div>
    <div v-if="selectedOption != DataExportType.None" class="max-h-48 overflow-auto">
      <table
        class="table table-zebra table-pin-rows w-full overflow-auto text-xs"
      >
        <thead class="border-b">
          <tr>
            <th>Keys Total</th>
            <th>Click Total</th>
            <th>Moved Distance</th>
            <th>Scroll Delta</th>
            <th>Start Timestamp</th>
            <th>End Timestamp</th>
          </tr>
        </thead>
        <tbody class="">
          <tr v-for="windowActivity in data" :key="windowActivity.id">
            <td>{{ windowActivity.keysTotal }}</td>
            <td>{{ windowActivity.clickTotal }}</td>
            <td>{{ windowActivity.movedDistance }}</td>
            <td>{{ windowActivity.scrollDelta }}</td>
            <td>{{ windowActivity.tsStart.toLocaleString() }}</td>
            <td>{{ windowActivity.tsEnd.toLocaleString() }}</td>
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
<style lang="less">
@import '../styles/index';
</style>
