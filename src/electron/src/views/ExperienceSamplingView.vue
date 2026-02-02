<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import { ref } from 'vue';

const esConfig = studyConfig.trackers.experienceSamplingTracker;
const studyQuestions = esConfig.questions;

const randomQuestionNr = Math.floor(Math.random() * studyQuestions.length);
const question = esConfig.questions[randomQuestionNr];
const questionLabels = esConfig.responseOptions[randomQuestionNr];
const scale = Array.from({ length: esConfig.scale }, (_, i) => i + 1);

const language =
  (typeof navigator !== 'undefined' &&
    (navigator.language || (navigator.languages && navigator.languages[0]))) ||
  'en';

const promptedAt = new Date();
const promptedAtString = new Intl.DateTimeFormat(language, {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  hourCycle: 'h23',
}).format(promptedAt);

const sampleLoadingValue = ref<number | null>();

async function createExperienceSample(value: number) {
  sampleLoadingValue.value = value;
  try {
    await Promise.all([
      typedIpcRenderer.invoke(
        'createExperienceSample',
        promptedAt,
        question,
        questionLabels.join(', '),
        esConfig.scale,
        value
      ),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', false);
  } catch (error) {
    console.error('Error creating team', error);
  }
}

async function skipExperienceSample() {
  sampleLoadingValue.value = null;
  try {
    await Promise.all([
      typedIpcRenderer.invoke(
        'createExperienceSample',
        promptedAt,
        question,
        questionLabels.join(', '),
        esConfig.scale,
        undefined,
        true
      ),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', true);
  } catch (error) {
    console.error('Error creating team', error);
  }
}
</script>
<template>
  <div class="experience-sampling-notification flex flex-col">
    <div class="notification-top-bar">
      <div>Self-Reflection: {{ studyConfig.name }}</div>
      <div>{{ promptedAtString }}</div>
    </div>
    <div class="pointer-events-auto flex h-full flex-row">
      <div class="flex-1 p-4 pt-1">
        <div class="flex-1">
          <p class="prompt">{{ question }}</p>
          <div class="-mx-1 mt-2 flex flex-row justify-between">
            <div
              v-for="value in scale"
              :key="value"
              class="sample-answer"
              @click="createExperienceSample(value)"
            >
              <span v-if="sampleLoadingValue !== value" class="mx-auto flex font-medium">
                {{ value }}
              </span>
              <span v-else class="mx-auto flex font-medium">
                <span class="loading loading-spinner loading-xs" />
              </span>
            </div>
          </div>
          <div class="mt-1 flex flex-row text-sm text-gray-400">
            <div class="basis-1/3">{{ questionLabels[0] }}</div>
            <div class="basis-1/3 text-center">
              <span v-if="questionLabels.length === 3">{{ questionLabels[1] }}</span>
            </div>
            <div class="basis-1/3 text-right">{{ questionLabels[2] || questionLabels[1] }}</div>
          </div>
        </div>
      </div>
      <div class="flex cursor-pointer border-l border-gray-200">
        <div
          class="flex w-full items-center justify-center rounded-none border border-transparent px-4 text-sm font-medium text-gray-600 hover:bg-gray-100 hover:text-gray-900 focus:outline-none"
          @click="skipExperienceSample()"
        >
          <span v-if="sampleLoadingValue !== null" class="w-6"> Skip </span>
          <span v-else class="w-6 font-medium">
            <span class="loading loading-spinner loading-xs" />
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
<style lang="less" scoped>
@import '../styles/index';
.experience-sampling-notification {
  @apply h-full bg-white;
  user-select: none;
  overflow: hidden;

  .notification-top-bar {
    @apply pointer-events-auto flex w-full flex-shrink-0 justify-between bg-gray-200 px-2 py-1 text-xs text-gray-500;
    line-height: 1.35rem;
    -webkit-app-region: drag;
  }

  .prompt {
    @apply font-bold;
    color: @primary-color;
  }

  .sample-answer {
    @apply mx-1 flex h-8 w-8 cursor-pointer items-center rounded-md border border-gray-200 bg-gray-100 text-center align-middle text-gray-500  transition-all hover:bg-gray-700 hover:text-white;
  }
}
</style>
