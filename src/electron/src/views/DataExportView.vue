<script setup lang="ts">
import StepIndicator from '../components/StepIndicator.vue';
import { computed, onMounted, ref } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import DataExportWindowActivityTracker from '../components/DataExportWindowActivityTracker.vue';
import { WindowActivityEntity } from '../../electron/main/entities/WindowActivityEntity';
import { UserInputEntity } from '../../electron/main/entities/UserInputEntity';
import DataExportUserInputTracker from '../components/DataExportUserInputTracker.vue';

const currentStep = ref(0);
const transitionName = ref('slide-lef-right');
const isLoading = ref(true);

const studyInfo = ref<StudyInfoDto>();
const mostRecentWindowActivities = ref<WindowActivityEntity[]>();
const mostRecentUserInputs = ref<UserInputEntity[]>();

const availableSteps = ['export-1', 'export-2', 'export-3'];

const maxSteps = computed(() => {
  return availableSteps.length;
});

const currentNamedStep = computed(() => {
  return availableSteps[currentStep.value];
});

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');
  mostRecentWindowActivities.value = await typedIpcRenderer.invoke(
    'getMostRecentWindowActivities',
    5
  );
  mostRecentUserInputs.value = await typedIpcRenderer.invoke(
    'getMostRecentUserInputs',
    5
  );
  isLoading.value = false;
});

async function handleNextStep() {
  if (currentStep.value === maxSteps.value - 1) {
    return;
  }
  transitionName.value = 'slide-left-right';
  currentStep.value++;
}

function handleBackStep() {
  if (currentStep.value === 0) {
    return;
  }
  transitionName.value = 'slide-right-left';
  currentStep.value--;
}
</script>

<template>
  <div class="h-screen p-5">
    <div v-if="!studyInfo" class="flex h-full w-full items-center justify-center overflow-y-scroll">
      <span class="loading loading-spinner loading-lg" />
    </div>
    <div v-else class="relative flex h-full flex-col justify-between text-neutral-400">
      <transition-group :name="transitionName">
        <div v-if="currentNamedStep === 'export-1'" key="0" class="flex w-full flex-col">
          <article class="prose prose-lg">
            <p>
              Thank you for participating in the {{ studyInfo.studyName }}-study! So far, all data
              that has been collected and stored
              <b class="dark:text-white">only locally on your machine</b>. In this step, the
              researchers would like to ask you to share this data for analysis and publication in
              scientific journals.
            </p>
            <p>
              Please click "Next" once you are ready to
              <b class="dark:text-white">first review, and later share your data</b>. The export
              that will be created with your permission in the next step will be encrypted and
              password-protected.
            </p>
            <p>
              Below, you find additional information on the study and how the researchers ensure
              your data privacy and security.
            </p>
            <table class="table-auto text-sm">
              <tbody>
                <tr>
                  <td class="w-40">Study Description:</td>
                  <td>
                    {{ studyInfo.shortDescription }}
                  </td>
                </tr>

                <tr>
                  <td>Contact:</td>
                  <td>{{ studyInfo.contactName }} ({{ studyInfo.contactEmail }})</td>
                </tr>
                <tr>
                  <td>Study Website:</td>
                  <td>
                    <a :href="studyInfo.infoUrl" target="_blank">{{ studyInfo.infoUrl }}</a>
                  </td>
                </tr>
                <tr>
                  <td>Privacy Policy:</td>
                  <td>
                    <a :href="studyInfo.privacyPolicyUrl" target="_blank">{{
                      studyInfo.privacyPolicyUrl
                    }}</a>
                  </td>
                </tr>
              </tbody>
            </table>
          </article>
        </div>
        <div
          v-if="currentNamedStep === 'export-2'"
          key="1"
          class="absolute h-5/6 w-full overflow-y-scroll"
        >
          <DataExportWindowActivityTracker
            v-if="studyConfig.trackers.windowActivityTracker.enabled"
            :study-info="studyInfo"
            :data="mostRecentWindowActivities"
            @change="console.log"
          />
          <DataExportUserInputTracker
            v-if="studyConfig.trackers.windowActivityTracker.enabled"
            :study-info="studyInfo"
            :data="mostRecentUserInputs"
            @change="console.log"
          />
        </div>
      </transition-group>

      <div class="flex-grow" />
      <div id="footer" class="z-10 mt-auto flex items-center justify-between">
        <button
          class="btn btn-outline"
          type="button"
          :disabled="currentStep === 0"
          @click="handleBackStep"
        >
          Back
        </button>
        <StepIndicator v-if="maxSteps > 1" :current-step="currentStep" :total-steps="maxSteps" />
        <button
          class="btn btn-active btn-md"
          type="button"
          :disabled="isLoading"
          @click="handleNextStep"
        >
          <template v-if="currentStep === maxSteps - 1">Close </template>
          <template v-else> Next </template>
        </button>
      </div>
    </div>
  </div>
</template>
<style lang="less" scoped></style>
