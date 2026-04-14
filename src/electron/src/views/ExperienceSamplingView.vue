<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import { computed, nextTick, onMounted, ref } from 'vue';
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

const trigger: 'manual' | 'auto' = new URLSearchParams(window.location.search).get('trigger') === 'manual' ? 'manual' : 'auto';
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

const needsSubmitButton = selectedQuestion.answerType === 'TextResponse' || selectedQuestion.answerType === 'MultiChoice';

const rootEl = ref<HTMLElement | null>(null);

async function measureAndResize() {
  await nextTick();
  const el = rootEl.value;
  if (!el) return;
  typedIpcRenderer.invoke('resizeExperienceSamplingWindow', Math.ceil(el.scrollHeight) + 2);
}

onMounted(() => {
  measureAndResize();
});

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

function buildResponseOptionsSnapshot(): string {
  if (selectedQuestion.answerType === 'LikertScale') {
    return JSON.stringify({
      type: 'LikertScale',
      scale: selectedQuestion.scale,
      labels: selectedQuestion.responseOptions
    });
  }
  if (selectedQuestion.answerType === 'TextResponse') {
    return JSON.stringify({
      type: 'TextResponse',
      inputType: selectedQuestion.responseOptions,
      maxLength: selectedQuestion.maxLength
    });
  }
  return JSON.stringify({
    type: selectedQuestion.answerType,
    options: selectedQuestion.responseOptions
  });
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
  createExperienceSample();
}

function onSingleChoiceDropdownChange(value: string) {
  if (selectedQuestion.answerType !== 'SingleChoice') {
    return;
  }
  singleChoiceResponse.value = value || null;
  if (singleChoiceResponse.value) {
    createExperienceSample();
  }
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
        buildResponseValue(answer),
        false,
        trigger
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
        true,
        trigger
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
  <div ref="rootEl" class="experience-sampling-notification flex flex-col">
    <div class="notification-top-bar">
      <div>Self-Reflection: {{ studyConfig.name }}</div>
      <div>{{ promptedAtString }}</div>
    </div>
    <div class="pointer-events-auto flex flex-row">
      <div class="flex flex-1 p-4 pt-1">
        <div class="flex flex-1 flex-col">
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

          <div v-if="selectedQuestion.answerType === 'LikertScale'" class="mt-1 flex flex-row text-sm text-gray-400 dark:text-gray-500">
            <div class="basis-1/3">{{ selectedQuestion.responseOptions[0] }}</div>
            <div class="basis-1/3 text-center">
              <span v-if="selectedQuestion.responseOptions.length === 3">{{ selectedQuestion.responseOptions[1] }}</span>
            </div>
            <div class="basis-1/3 text-right">
              {{ selectedQuestion.responseOptions[2] || selectedQuestion.responseOptions[1] }}
            </div>
          </div>

          <div v-if="selectedQuestion.answerType === 'TextResponse'" class="mt-2 flex flex-col">
            <div class="text-answer-content">
              <div v-if="textMode === 'singleLine'" class="text-answer-wrapper">
                <input
                  v-model="textResponse"
                  class="text-answer-input"
                  :maxlength="textMaxLength"
                  type="text"
                />
                <span class="char-counter">{{ textResponse.length }}/{{ textMaxLength }}</span>
              </div>
              <div v-else class="text-answer-wrapper text-answer-wrapper-multi">
                <textarea
                  v-model="textResponse"
                  class="text-answer-textarea"
                  :maxlength="textMaxLength"
                />
                <span class="char-counter">{{ textResponse.length }}/{{ textMaxLength }}</span>
              </div>
            </div>
          </div>

          <div
            v-if="selectedQuestion.answerType === 'SingleChoice' || selectedQuestion.answerType === 'MultiChoice'"
            class="mt-1 flex flex-col"
          >
            <div class="choice-hint">
              {{ selectedQuestion.answerType === 'SingleChoice' ? 'Pick one' : 'Pick one or more' }}
            </div>
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

              <div v-else>
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
          </div>
        </div>
      </div>
      <div class="flex cursor-pointer border-l border-gray-200 dark:border-gray-600 self-stretch">
        <div class="flex w-full flex-col items-center justify-center">
          <button
            v-if="needsSubmitButton"
            class="submit-side-button"
            :disabled="!isAnswerReady || isSubmitting"
            @click="createExperienceSample()"
          >
            <span v-if="!(isSubmitting && submitMode === 'answer')">Submit</span>
            <span v-else class="loading loading-spinner loading-xs" />
          </button>
          <div
            class="skip-button"
            @click="!isSubmitting && skipExperienceSample()"
          >
            <span v-if="!(isSubmitting && submitMode === 'skip')">Skip</span>
            <span v-else class="loading loading-spinner loading-xs" />
          </div>
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

  .text-answer-wrapper {
    position: relative;
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

  @media (prefers-color-scheme: dark) {
    .text-answer-input,
    .text-answer-textarea {
      border-color: #4b5563;
      background: #374151;
      color: #e5e7eb;
    }
  }

  .text-answer-input {
    height: 2.25rem;
    padding-right: 4.5rem;
  }

  .text-answer-textarea {
    height: 14rem;
    min-height: 14rem;
    max-height: 14rem;
    resize: none;
    overflow-y: auto;
    padding-bottom: 1.5rem;
  }

  .text-answer-input:focus,
  .text-answer-textarea:focus {
    border-color: #93c5fd;
    box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
  }

  .char-counter {
    position: absolute;
    font-size: 0.675rem;
    color: #9ca3af;
    pointer-events: none;
  }

  .text-answer-wrapper:not(.text-answer-wrapper-multi) .char-counter {
    right: 0.5rem;
    top: 50%;
    transform: translateY(-50%);
  }

  .text-answer-wrapper-multi .char-counter {
    right: 0.625rem;
    bottom: 0.375rem;
  }

  .text-answer-content,
  .choice-answer-content {
    padding-right: 0.25rem;
  }

  .choice-hint {
    font-size: 0.7rem;
    color: #9ca3af;
    margin-bottom: 0.35rem;
  }

  @media (prefers-color-scheme: dark) {
    .choice-hint {
      color: #6b7280;
    }
  }

  .choice-list {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0.35rem;
  }

  .choice-option {
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #f3f4f6;
    color: #374151;
    text-align: left;
    padding: 0.3rem 0.625rem;
    font-size: 0.8rem;
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

  @media (prefers-color-scheme: dark) {
    .choice-option {
      border-color: #4b5563;
      background: #374151;
      color: #d1d5db;
    }

    .choice-option:hover {
      background: #4b5563;
      color: #f3f4f6;
    }

    .choice-option-selected {
      background: #60a5fa;
      border-color: #60a5fa;
      color: #ffffff;
    }
  }

  .choice-select {
    width: 100%;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #1f2937;
    padding: 0.45rem 0.625rem;
    font-size: 0.8rem;
    outline: none;
  }

  .choice-select:focus {
    border-color: #93c5fd;
    box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
  }

  @media (prefers-color-scheme: dark) {
    .choice-select {
      border-color: #4b5563;
      background: #374151;
      color: #e5e7eb;
    }
  }

  .choice-select-multi {
    min-height: 10rem;
    padding: 0.3rem;
  }

  .submit-side-button {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    padding: 0.5rem 0.75rem;
    font-size: 0.8rem;
    font-weight: 600;
    color: #ffffff;
    background: @primary-color;
    border: none;
    cursor: pointer;
    transition: opacity 120ms ease;
  }

  .submit-side-button:hover:not(:disabled) {
    opacity: 0.85;
  }

  .submit-side-button:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .skip-button {
    flex: 1;
    display: flex;
    width: 100%;
    align-items: center;
    justify-content: center;
    padding: 0 1rem;
    font-size: 0.875rem;
    font-weight: 500;
    color: #6b7280;
    cursor: pointer;
    transition: background-color 120ms ease, color 120ms ease;
  }

  .skip-button:hover {
    background: #f3f4f6;
    color: #111827;
  }

  @media (prefers-color-scheme: dark) {
    .skip-button {
      color: #9ca3af;
    }

    .skip-button:hover {
      background: #374151;
      color: #e5e7eb;
    }
  }
}
</style>
