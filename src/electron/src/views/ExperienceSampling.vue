<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';

const esConfig = studyConfig.trackers.experienceSampling;
const studyQuestions = esConfig.questions;

const randomQuestionNr = Math.floor(Math.random() * studyQuestions.length);
const question = esConfig.questions[randomQuestionNr];
const questionLabels = esConfig.responseOptions[randomQuestionNr];
const scale = Array.from({ length: esConfig.scale }, (_, i) => i + 1);

const promptedAt = new Date(Date.now());
const promptedAtString = promptedAt.toLocaleTimeString().substring(0, 5);

async function createExperienceSample(value: number) {
  try {
    await typedIpcRenderer.invoke('createExperienceSample', promptedAt.getTime(), question, value);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow');
  } catch (error) {
    console.error('Error creating team', error);
  }
}
</script>
<template>
  <div class="experience-sampling-notification">
    <div class="notification-top-bar">
      <div>Self-Report: {{ studyConfig.name }}</div>
      <div>{{ promptedAtString }}</div>
    </div>
    <div class="pointer-events-auto flex w-full">
      <div class="w-0 flex-1 p-4 pt-1">
        <div class="flex items-start">
          <div class="w-0 flex-1">
            <p class="prompt">{{ question }}</p>
            <div class="-mx-2 mt-2 flex flex-row justify-between">
              <div
                v-for="value in scale"
                :key="value"
                class="sample-answer"
                @click="createExperienceSample(value)"
              >
                <span v-if="true" class="mx-auto flex font-medium">
                  {{ value }}
                </span>
              </div>
            </div>
            <div class="mt-1 flex flex-row text-sm text-gray-400">
              <div>{{ questionLabels[0] }}</div>
              <div class="mx-auto">
                <span v-if="questionLabels.length === 3">{{ questionLabels[1] }}</span>
              </div>
              <div>{{ questionLabels[2] || questionLabels[1] }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="flex cursor-pointer border-l border-gray-200">
        <div
          class="flex w-full items-center justify-center rounded-none rounded-r-lg border border-transparent p-4 text-sm font-medium text-gray-600 hover:text-gray-900 focus:outline-none"
        >
          Skip
        </div>
      </div>
    </div>
  </div>
</template>
<style lang="less" scoped>
.experience-sampling-notification {
  user-select: none;
  overflow: hidden;

  .notification-top-bar {
    @apply pointer-events-auto flex w-full justify-between bg-gray-200 px-2 py-1 text-xs text-gray-500;
    line-height: 1.35rem;
    -webkit-app-region: drag;
  }

  .prompt {
    @apply font-medium text-gray-900;
  }

  .sample-answer {
    @apply mx-1 flex h-8 w-8 cursor-pointer items-center rounded-md border border-gray-200 bg-gray-100 text-center align-middle text-gray-500 outline-none ring-2 ring-gray-900 transition-all hover:bg-gray-700 hover:text-white;
  }
}
</style>
