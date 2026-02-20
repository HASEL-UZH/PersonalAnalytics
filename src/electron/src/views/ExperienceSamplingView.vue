<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import { computed, ref } from 'vue';
import type { ExperienceSamplingQuestion } from '../../shared/StudyConfiguration';

const esConfig = studyConfig.trackers.experienceSamplingTracker;
const studyQuestions = esConfig.questions;

const randomQuestionNr = Math.floor(Math.random() * studyQuestions.length);
const selectedQuestion: ExperienceSamplingQuestion = esConfig.questions[randomQuestionNr];
const scale =
  selectedQuestion.answerType === 'LikertScale'
    ? Array.from({ length: selectedQuestion.scale }, (_, i) => i + 1)
    : [];
const choiceOptions =
  selectedQuestion.answerType === 'SingleChoice' || selectedQuestion.answerType === 'MultiChoice'
    ? selectedQuestion.responseOptions
    : [];
const useChoiceDropdown = computed(() => choiceOptions.length >= 10);
const choiceSelectSize = computed(() => Math.min(Math.max(choiceOptions.length, 6), 10));

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

const isSubmitting = ref(false);
const submitMode = ref<'answer' | 'skip' | null>(null);
const textResponse = ref('');
const singleChoiceResponse = ref<string | null>(null);
const multiChoiceResponse = ref<string[]>([]);

const textMode = computed(() => {
  return selectedQuestion.answerType === 'TextResponse' ? selectedQuestion.responseOptions : 'singleLine';
});

const textMaxLength = computed(() => {
  return selectedQuestion.answerType === 'TextResponse' ? selectedQuestion.maxLength : 0;
});

const isAnswerReady = computed(() => {
  if (selectedQuestion.answerType === 'TextResponse') {
    return textResponse.value.trim().length > 0;
  }
  if (selectedQuestion.answerType === 'SingleChoice') {
    return singleChoiceResponse.value !== null;
  }
  if (selectedQuestion.answerType === 'MultiChoice') {
    return multiChoiceResponse.value.length > 0;
  }
  return false;
});

function buildResponseOptionsSnapshot(): string | null {
  if (selectedQuestion.answerType === 'LikertScale') {
    return JSON.stringify(selectedQuestion.responseOptions);
  }
  if (selectedQuestion.answerType === 'TextResponse') {
    return JSON.stringify({
      inputType: selectedQuestion.responseOptions,
      maxLength: selectedQuestion.maxLength
    });
  }
  return JSON.stringify(selectedQuestion.responseOptions);
}

function buildResponseValue(answer?: number): string | undefined {
  if (selectedQuestion.answerType === 'LikertScale') {
    return answer?.toString();
  }
  if (selectedQuestion.answerType === 'TextResponse') {
    const trimmed = textResponse.value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
  if (selectedQuestion.answerType === 'SingleChoice') {
    return singleChoiceResponse.value ?? undefined;
  }
  if (selectedQuestion.answerType === 'MultiChoice') {
    return multiChoiceResponse.value.length > 0 ? JSON.stringify(multiChoiceResponse.value) : undefined;
  }
  return undefined;
}

function toggleMultiChoiceOption(option: string) {
  if (selectedQuestion.answerType !== 'MultiChoice') {
    return;
  }
  if (multiChoiceResponse.value.includes(option)) {
    multiChoiceResponse.value = multiChoiceResponse.value.filter((item) => item !== option);
  } else {
    multiChoiceResponse.value = [...multiChoiceResponse.value, option];
  }
}

function selectSingleChoiceOption(option: string) {
  if (selectedQuestion.answerType !== 'SingleChoice') {
    return;
  }
  singleChoiceResponse.value = option;
}

function onSingleChoiceDropdownChange(value: string) {
  if (selectedQuestion.answerType !== 'SingleChoice') {
    return;
  }
  singleChoiceResponse.value = value || null;
}

function onMultiChoiceDropdownChange(event: Event) {
  if (selectedQuestion.answerType !== 'MultiChoice') {
    return;
  }
  const selected = Array.from((event.target as HTMLSelectElement).selectedOptions).map(
    (option) => option.value
  );
  multiChoiceResponse.value = selected;
}

async function createExperienceSample(answer?: number) {
  isSubmitting.value = true;
  submitMode.value = 'answer';
  try {
    await Promise.all([
      typedIpcRenderer.invoke(
        'createExperienceSample',
        promptedAt,
        selectedQuestion.question,
        selectedQuestion.answerType,
        buildResponseOptionsSnapshot(),
        selectedQuestion.answerType === 'LikertScale' ? selectedQuestion.scale : null,
        buildResponseValue(answer)
      ),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', false);
  } catch (error) {
    console.error('Error creating team', error);
  } finally {
    isSubmitting.value = false;
    submitMode.value = null;
  }
}

async function skipExperienceSample() {
  isSubmitting.value = true;
  submitMode.value = 'skip';
  try {
    await Promise.all([
      typedIpcRenderer.invoke(
        'createExperienceSample',
        promptedAt,
        selectedQuestion.question,
        selectedQuestion.answerType,
        buildResponseOptionsSnapshot(),
        selectedQuestion.answerType === 'LikertScale' ? selectedQuestion.scale : null,
        undefined,
        true
      ),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', true);
  } catch (error) {
    console.error('Error creating team', error);
  } finally {
    isSubmitting.value = false;
    submitMode.value = null;
  }
}
</script>
<template>
  <div class="experience-sampling-notification flex min-h-screen flex-col">
    <div class="notification-top-bar">
      <div>Self-Reflection: {{ studyConfig.name }}</div>
      <div>{{ promptedAtString }}</div>
    </div>
    <div class="pointer-events-auto flex min-h-0 flex-1 flex-row">
      <div class="flex min-h-0 flex-1 p-4 pt-1">
        <div class="flex min-h-0 flex-1 flex-col">
          <p class="prompt">{{ selectedQuestion.question }}</p>

          <div v-if="selectedQuestion.answerType === 'LikertScale'" class="-mx-1 mt-2 flex flex-row justify-between">
            <div
              v-for="value in scale"
              :key="value"
              class="sample-answer"
              @click="!isSubmitting && createExperienceSample(value)"
            >
              <span v-if="!(isSubmitting && submitMode === 'answer')" class="mx-auto flex font-medium">
                {{ value }}
              </span>
              <span v-else class="mx-auto flex font-medium">
                <span class="loading loading-spinner loading-xs" />
              </span>
            </div>
          </div>

          <div v-if="selectedQuestion.answerType === 'LikertScale'" class="mt-1 flex flex-row text-sm text-gray-400">
            <div class="basis-1/3">{{ selectedQuestion.responseOptions[0] }}</div>
            <div class="basis-1/3 text-center">
              <span v-if="selectedQuestion.responseOptions.length === 3">{{ selectedQuestion.responseOptions[1] }}</span>
            </div>
            <div class="basis-1/3 text-right">
              {{ selectedQuestion.responseOptions[2] || selectedQuestion.responseOptions[1] }}
            </div>
          </div>

          <div v-if="selectedQuestion.answerType === 'TextResponse'" class="mt-2 flex min-h-0 flex-1 flex-col">
            <div class="text-answer-content">
              <input
                v-if="textMode === 'singleLine'"
                v-model="textResponse"
                class="text-answer-input"
                :maxlength="textMaxLength"
                type="text"
              />
              <textarea
                v-else
                v-model="textResponse"
                class="text-answer-textarea"
                :maxlength="textMaxLength"
              />
              <div class="mt-1 text-right text-xs text-gray-500">
                {{ textResponse.length }} / {{ textMaxLength }}
              </div>
            </div>
            <button
              class="action-button mt-2 self-start flex-shrink-0"
              :disabled="!isAnswerReady || isSubmitting"
              @click="createExperienceSample()"
            >
              <span v-if="!(isSubmitting && submitMode === 'answer')">Submit</span>
              <span v-else class="loading loading-spinner loading-xs" />
            </button>
          </div>

          <div
            v-if="selectedQuestion.answerType === 'SingleChoice' || selectedQuestion.answerType === 'MultiChoice'"
            class="mt-2 flex min-h-0 flex-1 flex-col"
          >
            <div class="choice-answer-content">
              <div v-if="!useChoiceDropdown" class="choice-list">
                <button
                  v-for="option in choiceOptions"
                  :key="option"
                  class="choice-option"
                  :class="{
                    'choice-option-selected': selectedQuestion.answerType === 'SingleChoice'
                      ? singleChoiceResponse === option
                      : multiChoiceResponse.includes(option)
                  }"
                  :disabled="isSubmitting"
                  @click="
                    selectedQuestion.answerType === 'SingleChoice'
                      ? selectSingleChoiceOption(option)
                      : toggleMultiChoiceOption(option)
                  "
                >
                  {{ option }}
                </button>
              </div>

              <div v-else class="pr-1">
                <select
                  v-if="selectedQuestion.answerType === 'SingleChoice'"
                  class="choice-select"
                  :value="singleChoiceResponse ?? ''"
                  :disabled="isSubmitting"
                  @change="onSingleChoiceDropdownChange(($event.target as HTMLSelectElement).value)"
                >
                  <option value="" disabled>Select an option</option>
                  <option v-for="option in choiceOptions" :key="option" :value="option">
                    {{ option }}
                  </option>
                </select>

                <select
                  v-else
                  class="choice-select choice-select-multi"
                  :size="choiceSelectSize"
                  multiple
                  :disabled="isSubmitting"
                  @change="onMultiChoiceDropdownChange($event)"
                >
                  <option
                    v-for="option in choiceOptions"
                    :key="option"
                    :value="option"
                    :selected="multiChoiceResponse.includes(option)"
                  >
                    {{ option }}
                  </option>
                </select>
              </div>
            </div>
            <button
              class="action-button mt-3 self-start flex-shrink-0"
              :disabled="!isAnswerReady || isSubmitting"
              @click="createExperienceSample()"
            >
              <span v-if="!(isSubmitting && submitMode === 'answer')">Submit</span>
              <span v-else class="loading loading-spinner loading-xs" />
            </button>
          </div>
        </div>
      </div>
      <div class="flex cursor-pointer border-l border-gray-200 self-stretch">
        <div
          class="flex w-full items-center justify-center rounded-none border border-transparent px-4 text-sm font-medium text-gray-600 hover:bg-gray-100 hover:text-gray-900 focus:outline-none"
          @click="!isSubmitting && skipExperienceSample()"
        >
          <span v-if="!(isSubmitting && submitMode === 'skip')" class="w-6"> Skip </span>
          <span v-else class="w-6 font-medium">
            <span class="loading loading-spinner loading-xs" />
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
<style lang="less" scoped>
@import '@/styles/index.less';
@import '../styles/tailwind-apply.css';
.experience-sampling-notification {
  .prompt {
    color: @primary-color;
  }

  .text-answer-input,
  .text-answer-textarea {
    width: 100%;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #1f2937;
    padding: 0.5rem 0.625rem;
    outline: none;
  }

  .text-answer-input {
    height: 2.25rem;
  }

  .text-answer-textarea {
    height: 9rem;
    min-height: 9rem;
    max-height: 9rem;
    resize: none;
    overflow-y: auto;
  }

  .text-answer-input:focus,
  .text-answer-textarea:focus {
    border-color: #93c5fd;
    box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
  }

  .text-answer-content,
  .choice-answer-content {
    min-height: 0;
    flex: 1 1 auto;
    padding-right: 0.25rem;
  }

  .text-answer-content {
    overflow: hidden;
  }

  .choice-answer-content {
    overflow-y: auto;
  }

  .choice-list {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0.5rem;
  }

  .choice-option {
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #f3f4f6;
    color: #374151;
    text-align: left;
    padding: 0.4rem 0.625rem;
    transition: background-color 120ms ease, color 120ms ease, border-color 120ms ease;
  }

  .choice-option:hover {
    background: #e5e7eb;
    color: #111827;
  }

  .choice-option-selected {
    background: #374151;
    border-color: #374151;
    color: #ffffff;
  }

  .choice-select {
    width: 100%;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #1f2937;
    padding: 0.45rem 0.625rem;
    outline: none;
  }

  .choice-select:focus {
    border-color: #93c5fd;
    box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
  }

  .choice-select-multi {
    min-height: 10rem;
    padding: 0.3rem;
  }

  .action-button {
    min-width: 5rem;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #f3f4f6;
    color: #374151;
    padding: 0.35rem 0.85rem;
    font-size: 0.875rem;
    font-weight: 600;
    transition: background-color 120ms ease, color 120ms ease;
  }

  .action-button:hover:not(:disabled) {
    background: #374151;
    color: #ffffff;
  }

  .action-button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
</style>
