<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import { computed, nextTick, onMounted, ref } from 'vue';
import type { ExperienceSamplingQuestion } from '../../shared/StudyConfiguration';
import type { DailySurveyResponseInput } from '../../electron/main/services/DailySurveyService';

const params = new URLSearchParams(window.location.search);
const samplingType = (params.get('samplingType') as 'morning' | 'evening') || 'evening';
const scheduledDateStr = params.get('scheduledDate');
const scheduledDate = scheduledDateStr ? new Date(scheduledDateStr) : null;

const surveyConfig = studyConfig.trackers.dailySurveyTracker?.surveys?.find(
  (s) => s.samplingType === samplingType
);
const questions: ExperienceSamplingQuestion[] = surveyConfig?.questions ?? [];
const requireAllAnswers = surveyConfig?.requireAllAnswers ?? false;

const language =
  (typeof navigator !== 'undefined' &&
    (navigator.language || (navigator.languages && navigator.languages[0]))) ||
  'en';

const promptedAt = new Date();
const isLateSubmission = scheduledDate && (promptedAt.getTime() - scheduledDate.getTime()) > 12 * 60 * 60 * 1000;
const scheduledDateString = scheduledDate
  ? new Intl.DateTimeFormat(language, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }).format(scheduledDate)
  : '';

const isSubmitting = ref(false);

const responses = ref<Record<number, { type: string; value: string | number | string[] | null }>>({});

function setResponse(index: number, type: string, value: string | number | string[] | null) {
  responses.value = { ...responses.value, [index]: { type, value } };
}

const allAnswered = computed(() => {
  return questions.every((_, i) => {
    const r = responses.value[i];
    if (!r) return false;
    if (r.value === null || r.value === '') return false;
    if (Array.isArray(r.value) && r.value.length === 0) return false;
    return true;
  });
});

const rootEl = ref<HTMLElement | null>(null);

async function measureAndResize() {
  await nextTick();
  const el = rootEl.value;
  if (!el) return;
  el.style.height = 'auto';
  const naturalHeight = el.scrollHeight;
  el.style.height = '100vh';
  typedIpcRenderer.invoke('resizeDailySurveyWindow', naturalHeight + 20);
}

onMounted(() => {
  measureAndResize();
});

function buildResponseOptionsSnapshot(q: ExperienceSamplingQuestion): string {
  if (q.answerType === 'LikertScale') {
    return JSON.stringify({ type: 'LikertScale', scale: q.scale, labels: q.responseOptions });
  }
  if (q.answerType === 'TextResponse') {
    return JSON.stringify({ type: 'TextResponse', inputType: q.responseOptions, maxLength: q.maxLength });
  }
  return JSON.stringify({ type: q.answerType, options: q.responseOptions });
}

async function submitSurvey() {
  isSubmitting.value = true;
  try {
    const responseInputs: DailySurveyResponseInput[] = questions.map((q, i) => {
      const r = responses.value[i];
      let responseValue: string | null = null;
      if (r && r.value !== null && r.value !== '') {
        responseValue = Array.isArray(r.value) ? JSON.stringify(r.value) : String(r.value);
      }
      return {
        question: q.question,
        answerType: q.answerType,
        responseOptions: buildResponseOptionsSnapshot(q),
        scale: q.answerType === 'LikertScale' ? q.scale : null,
        response: responseValue,
        skipped: !r || r.value === null || r.value === '' || (Array.isArray(r.value) && r.value.length === 0)
      };
    });

    await typedIpcRenderer.invoke('createDailySurveyResponses', promptedAt, samplingType, responseInputs);
    await typedIpcRenderer.invoke('closeDailySurveyWindow', false);
  } catch (error) {
    console.error('Error submitting daily survey', error);
  } finally {
    isSubmitting.value = false;
  }
}

async function skipSurvey() {
  isSubmitting.value = true;
  try {
    const responseInputs: DailySurveyResponseInput[] = questions.map((q) => ({
      question: q.question,
      answerType: q.answerType,
      responseOptions: buildResponseOptionsSnapshot(q),
      scale: q.answerType === 'LikertScale' ? q.scale : null,
      response: null,
      skipped: true
    }));

    await typedIpcRenderer.invoke('createDailySurveyResponses', promptedAt, samplingType, responseInputs);
    await typedIpcRenderer.invoke('closeDailySurveyWindow', true);
  } catch (error) {
    console.error('Error skipping daily survey', error);
  } finally {
    isSubmitting.value = false;
  }
}

async function postpone(minutes: number) {
  await typedIpcRenderer.invoke('postponeDailySurvey', samplingType, minutes);
}
</script>
<template>
  <div ref="rootEl" class="daily-survey flex flex-col">
    <div class="survey-header">
      <div class="survey-title">
        {{ samplingType === 'morning' ? 'Start-of-Workday' : 'End-of-Workday' }} Questionnaire
      </div>
    </div>

    <div class="postpone-bar">
      <button class="postpone-button" :disabled="isSubmitting" @click="postpone(5)">Remind me again in 5 minutes</button>
      <button class="postpone-button" :disabled="isSubmitting" @click="postpone(15)">Remind me again in 15 minutes</button>
      <button class="postpone-button" :disabled="isSubmitting" @click="postpone(60)">Remind me again in 60 minutes</button>
      <button class="postpone-button" :disabled="isSubmitting" @click="skipSurvey()">Skip</button>
    </div>

    <div v-if="isLateSubmission" class="late-notice">
      This survey was originally scheduled for {{ scheduledDateString }}.
    </div>

    <div class="survey-instructions">
      For the following questions and statements, please consider <strong>only this past work day</strong>:
    </div>

    <div class="questions-container">
      <div v-for="(q, index) in questions" :key="index" class="question-block">
        <p class="question-text">{{ q.question }}</p>

        <div v-if="q.answerType === 'LikertScale'" class="likert-container">
          <div class="likert-buttons">
            <button
              v-for="value in Array.from({ length: q.scale }, (_, i) => i + 1)"
              :key="value"
              class="likert-button"
              :class="{ 'likert-button-selected': responses[index]?.value === value }"
              :disabled="isSubmitting"
              @click="setResponse(index, 'LikertScale', value)"
            >
              {{ value }}
            </button>
          </div>
          <div class="likert-labels">
            <span>{{ q.responseOptions[0] }}</span>
            <span v-if="q.responseOptions.length === 3" class="text-center">{{ q.responseOptions[1] }}</span>
            <span class="text-right">{{ q.responseOptions[q.responseOptions.length - 1] }}</span>
          </div>
        </div>

        <div v-if="q.answerType === 'TextResponse'" class="text-container">
          <input
            v-if="q.responseOptions === 'singleLine'"
            class="text-input"
            type="text"
            :maxlength="q.maxLength"
            :value="(responses[index]?.value as string) ?? ''"
            :disabled="isSubmitting"
            @input="setResponse(index, 'TextResponse', ($event.target as HTMLInputElement).value)"
          />
          <textarea
            v-else
            class="text-textarea"
            :maxlength="q.maxLength"
            :value="(responses[index]?.value as string) ?? ''"
            :disabled="isSubmitting"
            @input="setResponse(index, 'TextResponse', ($event.target as HTMLTextAreaElement).value)"
          />
          <span v-if="q.maxLength" class="char-count">{{ ((responses[index]?.value as string) ?? '').length }} / {{ q.maxLength }}</span>
        </div>

        <div v-if="q.answerType === 'SingleChoice'" class="choice-container">
          <button
            v-for="option in q.responseOptions"
            :key="option"
            class="choice-button"
            :class="{ 'choice-button-selected': responses[index]?.value === option }"
            :disabled="isSubmitting"
            @click="setResponse(index, 'SingleChoice', option)"
          >
            {{ option }}
          </button>
        </div>

        <div v-if="q.answerType === 'MultiChoice'" class="choice-container">
          <span class="multi-choice-hint">Multiple selections possible</span>
          <button
            v-for="option in q.responseOptions"
            :key="option"
            class="choice-button"
            :class="{ 'choice-button-selected': (responses[index]?.value as string[] ?? []).includes(option) }"
            :disabled="isSubmitting"
            @click="() => {
              const current = (responses[index]?.value as string[]) ?? [];
              const updated = current.includes(option)
                ? current.filter((o) => o !== option)
                : [...current, option];
              setResponse(index, 'MultiChoice', updated);
            }"
          >
            {{ option }}
          </button>
        </div>
      </div>
    </div>

    <div class="survey-footer">
      <button
        class="save-button"
        :disabled="(requireAllAnswers && !allAnswered) || isSubmitting"
        @click="submitSurvey()"
      >
        <span v-if="!isSubmitting">Save</span>
        <span v-else class="loading loading-spinner loading-xs" />
      </button>
    </div>
  </div>
</template>
<style lang="less" scoped>
@import '@/styles/index.less';
@import '../styles/tailwind-apply.css';

.daily-survey {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
  background: #ffffff;
  color: #1f2937;
  height: 100vh;
  display: flex;
  flex-direction: column;

  @media (prefers-color-scheme: dark) {
    background: #1f2937;
    color: #e5e7eb;
  }

  .survey-header {
    padding: 0.75rem 1rem;
    background: #f3f4f6;
    border-bottom: 1px solid #e5e7eb;

    @media (prefers-color-scheme: dark) {
      background: #374151;
      border-bottom-color: #4b5563;
    }
  }

  .survey-title {
    font-weight: 700;
    font-size: 1.1rem;
  }

  .postpone-bar {
    display: flex;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid #e5e7eb;

    @media (prefers-color-scheme: dark) {
      border-bottom-color: #4b5563;
    }
  }

  .postpone-button {
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #374151;
    padding: 0.35rem 0.75rem;
    font-size: 0.8rem;
    cursor: pointer;
    transition: background-color 120ms ease;

    &:hover:not(:disabled) {
      background: #f3f4f6;
    }

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    @media (prefers-color-scheme: dark) {
      border-color: #4b5563;
      background: #374151;
      color: #d1d5db;

      &:hover:not(:disabled) {
        background: #4b5563;
      }
    }
  }

  .late-notice {
    padding: 0.5rem 1rem;
    background: #fef3c7;
    color: #92400e;
    font-size: 0.85rem;
    border-bottom: 1px solid #fde68a;

    @media (prefers-color-scheme: dark) {
      background: #78350f;
      color: #fde68a;
      border-bottom-color: #92400e;
    }
  }

  .survey-instructions {
    padding: 0.75rem 1rem;
    font-size: 0.9rem;
    color: #6b7280;

    @media (prefers-color-scheme: dark) {
      color: #9ca3af;
    }
  }

  .questions-container {
    padding: 0 1rem;
    flex: 1;
    overflow-y: auto;
  }

  .question-block {
    margin-bottom: 1.25rem;
  }

  .question-text {
    font-weight: 600;
    font-size: 0.95rem;
    margin-bottom: 0.5rem;
  }

  .likert-container {
    display: flex;
    flex-direction: column;
  }

  .likert-buttons {
    display: flex;
    gap: 0.35rem;
  }

  .likert-button {
    flex: 1;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #f3f4f6;
    color: #374151;
    padding: 0.4rem 0;
    font-size: 0.85rem;
    font-weight: 500;
    cursor: pointer;
    text-align: center;
    transition: background-color 120ms ease, color 120ms ease;

    &:hover:not(:disabled) {
      background: #e5e7eb;
    }

    @media (prefers-color-scheme: dark) {
      border-color: #4b5563;
      background: #374151;
      color: #d1d5db;

      &:hover:not(:disabled) {
        background: #4b5563;
      }
    }
  }

  .likert-button-selected {
    background: #374151;
    border-color: #374151;
    color: #ffffff;

    @media (prefers-color-scheme: dark) {
      background: #60a5fa;
      border-color: #60a5fa;
      color: #ffffff;
    }
  }

  .likert-labels {
    display: flex;
    justify-content: space-between;
    margin-top: 0.25rem;
    font-size: 0.75rem;
    color: #9ca3af;

    @media (prefers-color-scheme: dark) {
      color: #6b7280;
    }
  }

  .text-container {
    display: flex;
    flex-direction: column;
  }

  .text-input {
    width: 100%;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #1f2937;
    padding: 0.5rem 0.625rem;
    font-size: 0.85rem;
    outline: none;

    &:focus {
      border-color: #93c5fd;
      box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
    }

    @media (prefers-color-scheme: dark) {
      border-color: #4b5563;
      background: #374151;
      color: #e5e7eb;
    }
  }

  .text-textarea {
    width: 100%;
    height: 5rem;
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #ffffff;
    color: #1f2937;
    padding: 0.5rem 0.625rem;
    font-size: 0.85rem;
    outline: none;
    resize: none;

    &:focus {
      border-color: #93c5fd;
      box-shadow: 0 0 0 2px rgb(147 197 253 / 0.25);
    }

    @media (prefers-color-scheme: dark) {
      border-color: #4b5563;
      background: #374151;
      color: #e5e7eb;
    }
  }

  .char-count {
    align-self: flex-end;
    font-size: 0.75rem;
    color: #9ca3af;
    margin-top: 0.25rem;

    @media (prefers-color-scheme: dark) {
      color: #6b7280;
    }
  }

  .multi-choice-hint {
    font-size: 0.8rem;
    color: #6b7280;
    margin-bottom: 0.25rem;
    width: 100%;

    @media (prefers-color-scheme: dark) {
      color: #9ca3af;
    }
  }

  .choice-container {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }

  .choice-button {
    border: 1px solid #d1d5db;
    border-radius: 0.375rem;
    background: #f3f4f6;
    color: #374151;
    padding: 0.35rem 0.75rem;
    font-size: 0.85rem;
    cursor: pointer;
    transition: background-color 120ms ease, color 120ms ease;

    &:hover:not(:disabled) {
      background: #e5e7eb;
    }

    @media (prefers-color-scheme: dark) {
      border-color: #4b5563;
      background: #374151;
      color: #d1d5db;

      &:hover:not(:disabled) {
        background: #4b5563;
      }
    }
  }

  .choice-button-selected {
    background: #374151;
    border-color: #374151;
    color: #ffffff;

    @media (prefers-color-scheme: dark) {
      background: #60a5fa;
      border-color: #60a5fa;
      color: #ffffff;
    }
  }

  .survey-footer {
    display: flex;
    justify-content: flex-end;
    padding: 0.75rem 1rem;
    border-top: 1px solid #e5e7eb;

    @media (prefers-color-scheme: dark) {
      border-top-color: #4b5563;
    }
  }

  .save-button {
    background: @primary-color;
    color: #ffffff;
    border: none;
    border-radius: 0.375rem;
    padding: 0.5rem 2rem;
    font-size: 0.9rem;
    font-weight: 600;
    cursor: pointer;
    transition: opacity 120ms ease;

    &:hover:not(:disabled) {
      opacity: 0.85;
    }

    &:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }
  }
}
</style>
