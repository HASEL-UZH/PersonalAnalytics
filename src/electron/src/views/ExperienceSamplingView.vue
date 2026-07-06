<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import { computed, nextTick, onMounted, ref } from 'vue';
import type {
  ExperienceSamplingAnswerType,
  ExperienceSamplingQuestion
} from '../../shared/StudyConfiguration';
import type { ExperienceSamplingResponseInput } from '../../shared/dto/ExperienceSamplingDto';

type ResponseValue = string | number | string[] | null;

const esConfig = studyConfig.trackers.experienceSamplingTracker;
const studyQuestions = esConfig.questions;
const showAllQuestionsTogether = (esConfig.showAllQuestionsTogether ?? false) === true;

const randomQuestionNr = Math.floor(Math.random() * studyQuestions.length);
const displayedQuestions = computed<ExperienceSamplingQuestion[]>(() => {
  if (showAllQuestionsTogether) {
    return studyQuestions;
  }

  const selectedQuestion = studyQuestions[randomQuestionNr];
  return selectedQuestion ? [selectedQuestion] : [];
});
const hasQuestions = computed(() => displayedQuestions.value.length > 0);

const language =
  (typeof navigator !== 'undefined' &&
    (navigator.language || (navigator.languages && navigator.languages[0]))) ||
  'en';

const trigger: 'manual' | 'auto' =
  new URLSearchParams(window.location.search).get('trigger') === 'manual' ? 'manual' : 'auto';
const promptedAt = new Date();
const promptedAtString = new Intl.DateTimeFormat(language, {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  hourCycle: 'h23'
}).format(promptedAt);

const isSubmitting = ref(false);
const submitMode = ref<'answer' | 'skip' | null>(null);
const responses = ref<Record<number, { type: ExperienceSamplingAnswerType; value: ResponseValue }>>(
  {}
);

const rootEl = ref<HTMLElement | null>(null);

async function measureAndResize() {
  await nextTick();
  const el = rootEl.value;
  if (!el) return;

  const topBar = el.querySelector<HTMLElement>('.notification-top-bar');
  const questionArea = el.querySelector<HTMLElement>('.questions-scroll');
  const actionSide = el.querySelector<HTMLElement>('.action-side');
  const contentHeight = Math.max(
    questionArea?.scrollHeight ?? 0,
    actionSide?.scrollHeight ?? 0
  );
  const naturalHeight = (topBar?.scrollHeight ?? 0) + contentHeight;
  typedIpcRenderer.invoke('resizeExperienceSamplingWindow', Math.ceil(naturalHeight) + 2);
}

onMounted(() => {
  measureAndResize();
});

const needsSubmitButton = computed(() => {
  if (!hasQuestions.value) {
    return false;
  }

  if (showAllQuestionsTogether) {
    return true;
  }

  const selectedQuestion = displayedQuestions.value[0];
  return (
    selectedQuestion?.answerType === 'TextResponse' ||
    selectedQuestion?.answerType === 'MultiChoice'
  );
});

const allDisplayedQuestionsAnswered = computed(() => {
  return (
    displayedQuestions.value.length > 0 &&
    displayedQuestions.value.every((question, index) => isAnswerReady(index, question))
  );
});

const isSubmitDisabled = computed(() => {
  if (showAllQuestionsTogether) {
    return !allDisplayedQuestionsAnswered.value;
  }

  const selectedQuestion = displayedQuestions.value[0];
  return selectedQuestion ? !isAnswerReady(0, selectedQuestion) : true;
});

function getScale(question: ExperienceSamplingQuestion): number[] {
  return question.answerType === 'LikertScale'
    ? Array.from({ length: question.scale }, (_, i) => i + 1)
    : [];
}

function getChoiceOptions(question: ExperienceSamplingQuestion): string[] {
  if (question.answerType === 'SingleChoice' || question.answerType === 'MultiChoice') {
    return question.responseOptions;
  }
  return [];
}

function useChoiceDropdown(question: ExperienceSamplingQuestion): boolean {
  return getChoiceOptions(question).length >= 10;
}

function choiceSelectSize(question: ExperienceSamplingQuestion): number {
  const choiceOptions = getChoiceOptions(question);
  return Math.min(Math.max(choiceOptions.length, 6), 10);
}

function setResponse(index: number, type: ExperienceSamplingAnswerType, value: ResponseValue) {
  responses.value = { ...responses.value, [index]: { type, value } };
}

function getTextResponse(index: number): string {
  const value = responses.value[index]?.value;
  return typeof value === 'string' ? value : '';
}

function getMultiChoiceResponse(index: number): string[] {
  const value = responses.value[index]?.value;
  return Array.isArray(value) ? value : [];
}

function isChoiceSelected(index: number, option: string): boolean {
  const value = responses.value[index]?.value;
  return Array.isArray(value) ? value.includes(option) : value === option;
}

function isAnswerReady(index: number, question: ExperienceSamplingQuestion): boolean {
  const value = responses.value[index]?.value;

  if (question.answerType === 'LikertScale') {
    return typeof value === 'number';
  }
  if (question.answerType === 'TextResponse') {
    return typeof value === 'string' && value.trim().length > 0;
  }
  if (question.answerType === 'SingleChoice') {
    return typeof value === 'string' && value.length > 0;
  }
  if (question.answerType === 'MultiChoice') {
    return Array.isArray(value) && value.length > 0;
  }
  return false;
}

function buildResponseOptionsSnapshot(question: ExperienceSamplingQuestion): string {
  if (question.answerType === 'LikertScale') {
    return JSON.stringify({
      type: 'LikertScale',
      scale: question.scale,
      labels: question.responseOptions
    });
  }
  if (question.answerType === 'TextResponse') {
    return JSON.stringify({
      type: 'TextResponse',
      inputType: question.responseOptions,
      maxLength: question.maxLength
    });
  }
  return JSON.stringify({
    type: question.answerType,
    options: question.responseOptions
  });
}

function formatResponseValue(
  question: ExperienceSamplingQuestion,
  value: ResponseValue | undefined
): string | null {
  if (value === null || typeof value === 'undefined') {
    return null;
  }

  if (question.answerType === 'LikertScale') {
    return typeof value === 'number' ? value.toString() : null;
  }
  if (question.answerType === 'TextResponse') {
    const trimmed = typeof value === 'string' ? value.trim() : '';
    return trimmed.length > 0 ? trimmed : null;
  }
  if (question.answerType === 'SingleChoice') {
    return typeof value === 'string' && value.length > 0 ? value : null;
  }
  if (question.answerType === 'MultiChoice') {
    return Array.isArray(value) && value.length > 0 ? JSON.stringify(value) : null;
  }
  return null;
}

function buildResponseInput(
  question: ExperienceSamplingQuestion,
  index: number,
  skipped: boolean
): ExperienceSamplingResponseInput {
  return {
    question: question.question,
    answerType: question.answerType,
    responseOptions: buildResponseOptionsSnapshot(question),
    scale: question.answerType === 'LikertScale' ? question.scale : null,
    response: skipped ? null : formatResponseValue(question, responses.value[index]?.value),
    skipped
  };
}

async function answerLikertQuestion(
  index: number,
  question: ExperienceSamplingQuestion,
  value: number
) {
  if (question.answerType !== 'LikertScale') {
    return;
  }

  setResponse(index, 'LikertScale', value);
  if (!showAllQuestionsTogether) {
    await createExperienceSample(index, question);
  }
}

async function selectSingleChoiceOption(
  index: number,
  question: ExperienceSamplingQuestion,
  option: string
) {
  if (question.answerType !== 'SingleChoice') {
    return;
  }

  setResponse(index, 'SingleChoice', option);
  if (!showAllQuestionsTogether) {
    await createExperienceSample(index, question);
  }
}

async function onSingleChoiceDropdownChange(
  index: number,
  question: ExperienceSamplingQuestion,
  value: string
) {
  if (question.answerType !== 'SingleChoice') {
    return;
  }

  if (!value) {
    setResponse(index, 'SingleChoice', null);
    return;
  }

  setResponse(index, 'SingleChoice', value);
  if (!showAllQuestionsTogether) {
    await createExperienceSample(index, question);
  }
}

function toggleMultiChoiceOption(
  index: number,
  question: ExperienceSamplingQuestion,
  option: string
) {
  if (question.answerType !== 'MultiChoice') {
    return;
  }

  const current = getMultiChoiceResponse(index);
  const updated = current.includes(option)
    ? current.filter((item) => item !== option)
    : [...current, option];
  setResponse(index, 'MultiChoice', updated);
}

function onMultiChoiceDropdownChange(
  index: number,
  question: ExperienceSamplingQuestion,
  event: Event
) {
  if (question.answerType !== 'MultiChoice') {
    return;
  }

  const selected = Array.from((event.target as HTMLSelectElement).selectedOptions).map(
    (option) => option.value
  );
  setResponse(index, 'MultiChoice', selected);
}

function onTextInput(index: number, event: Event) {
  setResponse(index, 'TextResponse', (event.target as HTMLInputElement).value);
}

async function createExperienceSample(index: number, question: ExperienceSamplingQuestion) {
  if (!isAnswerReady(index, question)) {
    return;
  }

  isSubmitting.value = true;
  submitMode.value = 'answer';
  try {
    const responseValue = formatResponseValue(question, responses.value[index]?.value);
    await Promise.all([
      typedIpcRenderer.invoke(
        'createExperienceSample',
        promptedAt,
        question.question,
        question.answerType,
        buildResponseOptionsSnapshot(question),
        question.answerType === 'LikertScale' ? question.scale : null,
        responseValue ?? undefined,
        false,
        trigger
      ),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', false);
  } catch (error) {
    console.error('Error creating experience sample', error);
  } finally {
    isSubmitting.value = false;
    submitMode.value = null;
  }
}

async function submitDisplayedQuestions() {
  if (!showAllQuestionsTogether) {
    const selectedQuestion = displayedQuestions.value[0];
    if (selectedQuestion) {
      await createExperienceSample(0, selectedQuestion);
    }
    return;
  }

  if (!allDisplayedQuestionsAnswered.value) {
    return;
  }

  isSubmitting.value = true;
  submitMode.value = 'answer';
  try {
    const responseInputs = displayedQuestions.value.map((question, index) =>
      buildResponseInput(question, index, false)
    );
    await Promise.all([
      typedIpcRenderer.invoke('createExperienceSamples', promptedAt, responseInputs, trigger),
      new Promise((resolve) => setTimeout(resolve, 150))
    ]);
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', false);
  } catch (error) {
    console.error('Error creating experience samples', error);
  } finally {
    isSubmitting.value = false;
    submitMode.value = null;
  }
}

async function skipExperienceSample() {
  isSubmitting.value = true;
  submitMode.value = 'skip';
  try {
    const questions = displayedQuestions.value;
    if (showAllQuestionsTogether) {
      const responseInputs = questions.map((question, index) =>
        buildResponseInput(question, index, true)
      );
      await Promise.all([
        typedIpcRenderer.invoke('createExperienceSamples', promptedAt, responseInputs, trigger),
        new Promise((resolve) => setTimeout(resolve, 150))
      ]);
    } else if (questions[0]) {
      const selectedQuestion = questions[0];
      await Promise.all([
        typedIpcRenderer.invoke(
          'createExperienceSample',
          promptedAt,
          selectedQuestion.question,
          selectedQuestion.answerType,
          buildResponseOptionsSnapshot(selectedQuestion),
          selectedQuestion.answerType === 'LikertScale' ? selectedQuestion.scale : null,
          undefined,
          true,
          trigger
        ),
        new Promise((resolve) => setTimeout(resolve, 150))
      ]);
    }
    await typedIpcRenderer.invoke('closeExperienceSamplingWindow', true);
  } catch (error) {
    console.error('Error skipping experience sample', error);
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
    <div class="question-shell pointer-events-auto flex flex-1 flex-row">
      <div class="questions-scroll flex flex-1 p-4 pt-1">
        <div v-if="hasQuestions" class="flex flex-1 flex-col">
          <div
            v-for="(question, index) in displayedQuestions"
            :key="`${index}-${question.question}`"
            class="question-block"
            :class="{ 'question-block-stacked': showAllQuestionsTogether }"
          >
            <p class="prompt">{{ question.question }}</p>

            <div v-if="question.answerType === 'LikertScale'" class="mt-2 flex flex-row gap-1.5">
              <button
                v-for="value in getScale(question)"
                :key="value"
                type="button"
                class="sample-answer"
                :class="{ 'sample-answer-selected': responses[index]?.value === value }"
                :disabled="isSubmitting"
                @click="answerLikertQuestion(index, question, value)"
              >
                <span
                  v-if="!(isSubmitting && submitMode === 'answer' && !showAllQuestionsTogether)"
                >
                  {{ value }}
                </span>
                <span v-else>
                  <span class="loading loading-spinner loading-xs" />
                </span>
              </button>
            </div>

            <div
              v-if="question.answerType === 'LikertScale'"
              class="mt-1 flex flex-row text-sm text-gray-400 dark:text-gray-500"
            >
              <div class="basis-1/3">{{ question.responseOptions[0] }}</div>
              <div class="basis-1/3 text-center">
                <span v-if="question.responseOptions.length === 3">
                  {{ question.responseOptions[1] }}
                </span>
              </div>
              <div class="basis-1/3 text-right">
                {{ question.responseOptions[2] || question.responseOptions[1] }}
              </div>
            </div>

            <div v-if="question.answerType === 'TextResponse'" class="mt-2 flex flex-col">
              <div class="text-answer-content">
                <div v-if="question.responseOptions === 'singleLine'" class="text-answer-wrapper">
                  <input
                    class="text-answer-input"
                    :maxlength="question.maxLength"
                    :value="getTextResponse(index)"
                    :disabled="isSubmitting"
                    type="text"
                    @input="onTextInput(index, $event)"
                  />
                  <span class="char-counter">
                    {{ getTextResponse(index).length }}/{{ question.maxLength }}
                  </span>
                </div>
                <div v-else class="text-answer-wrapper text-answer-wrapper-multi">
                  <textarea
                    class="text-answer-textarea"
                    :maxlength="question.maxLength"
                    :value="getTextResponse(index)"
                    :disabled="isSubmitting"
                    @input="onTextInput(index, $event)"
                  />
                  <span class="char-counter">
                    {{ getTextResponse(index).length }}/{{ question.maxLength }}
                  </span>
                </div>
              </div>
            </div>

            <div
              v-if="question.answerType === 'SingleChoice' || question.answerType === 'MultiChoice'"
              class="mt-1 flex flex-col"
            >
              <div class="choice-hint">
                {{ question.answerType === 'SingleChoice' ? 'Pick one' : 'Pick one or more' }}
              </div>
              <div class="choice-answer-content">
                <div v-if="!useChoiceDropdown(question)" class="choice-list">
                  <button
                    v-for="option in getChoiceOptions(question)"
                    :key="option"
                    class="choice-option"
                    :class="{ 'choice-option-selected': isChoiceSelected(index, option) }"
                    :disabled="isSubmitting"
                    @click="
                      question.answerType === 'SingleChoice'
                        ? selectSingleChoiceOption(index, question, option)
                        : toggleMultiChoiceOption(index, question, option)
                    "
                  >
                    {{ option }}
                  </button>
                </div>

                <div v-else>
                  <select
                    v-if="question.answerType === 'SingleChoice'"
                    class="choice-select"
                    :value="(responses[index]?.value as string) ?? ''"
                    :disabled="isSubmitting"
                    @change="
                      onSingleChoiceDropdownChange(
                        index,
                        question,
                        ($event.target as HTMLSelectElement).value
                      )
                    "
                  >
                    <option value="" disabled>Select an option</option>
                    <option
                      v-for="option in getChoiceOptions(question)"
                      :key="option"
                      :value="option"
                    >
                      {{ option }}
                    </option>
                  </select>

                  <select
                    v-else
                    class="choice-select choice-select-multi"
                    :size="choiceSelectSize(question)"
                    multiple
                    :disabled="isSubmitting"
                    @change="onMultiChoiceDropdownChange(index, question, $event)"
                  >
                    <option
                      v-for="option in getChoiceOptions(question)"
                      :key="option"
                      :value="option"
                      :selected="getMultiChoiceResponse(index).includes(option)"
                    >
                      {{ option }}
                    </option>
                  </select>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-else class="empty-state">No self-reflection questions configured.</div>
      </div>
      <div
        class="action-side flex cursor-pointer self-stretch border-l border-gray-200 dark:border-gray-600"
      >
        <div class="flex w-full flex-col items-center justify-center">
          <button
            v-if="needsSubmitButton"
            class="submit-side-button"
            :disabled="isSubmitDisabled || isSubmitting"
            @click="submitDisplayedQuestions()"
          >
            <span v-if="!(isSubmitting && submitMode === 'answer')">
              {{ showAllQuestionsTogether ? 'Save' : 'Submit' }}
            </span>
            <span v-else class="loading loading-spinner loading-xs" />
          </button>
          <div class="skip-button" @click="!isSubmitting && skipExperienceSample()">
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
  height: 100vh;
  overflow: hidden;

  .question-shell {
    min-height: 0;
  }

  .questions-scroll {
    min-width: 0;
    min-height: 0;
    overflow-y: auto;
  }

  .question-block-stacked {
    padding-bottom: 0.85rem;
  }

  .question-block-stacked + .question-block-stacked {
    border-top: 1px solid #e5e7eb;
    padding-top: 0.85rem;
  }

  @media (prefers-color-scheme: dark) {
    .question-block-stacked + .question-block-stacked {
      border-top-color: #4b5563;
    }
  }

  .prompt {
    color: @primary-color;
  }

  .sample-answer-selected {
    border-color: @primary-color;
    background: @primary-color;
    color: #ffffff;
  }

  .sample-answer-selected:hover {
    background: @primary-color;
    color: #ffffff;
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
    transition:
      background-color 120ms ease,
      color 120ms ease,
      border-color 120ms ease;
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
    transition:
      background-color 120ms ease,
      color 120ms ease;
  }

  .skip-button:hover {
    background: #f3f4f6;
    color: #111827;
  }

  .empty-state {
    display: flex;
    min-height: 5rem;
    flex: 1;
    align-items: center;
    color: #6b7280;
    font-size: 0.875rem;
  }

  @media (prefers-color-scheme: dark) {
    .skip-button {
      color: #9ca3af;
    }

    .skip-button:hover {
      background: #374151;
      color: #e5e7eb;
    }

    .empty-state {
      color: #9ca3af;
    }
  }
}
</style>
