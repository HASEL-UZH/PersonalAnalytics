<script setup lang="ts">
import StepIndicator from '../components/StepIndicator.vue';
import { computed, onMounted, ref } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import typedIpcRenderer from '../utils/typedIpcRenderer';
import studyConfig from '../../shared/study.config';
import DataExportWindowActivityTracker from '../components/DataExportWindowActivityTracker.vue';
import DataExportUserInputTracker from '../components/DataExportUserInputTracker.vue';
import { DataExportType } from '../../shared/DataExportType.enum';
import WindowActivityDto from '../../shared/dto/WindowActivityDto';
import UserInputDto from '../../shared/dto/UserInputDto';

const currentStep = ref(0);
const transitionName = ref('slide-lef-right');
const isLoading = ref(true);

const studyInfo = ref<StudyInfoDto>();

const mostRecentUserInputs = ref<UserInputDto[]>();
const mostRecentWindowActivities = ref<WindowActivityDto[]>();
const mostRecentWindowActivitiesObfuscated = ref<WindowActivityDto[]>();
const obfuscateWindowActivities = ref(false);

const exportWindowActivitySelectedOption = ref<DataExportType>(DataExportType.None);
const exportUserInputSelectedOption = ref<DataExportType>(DataExportType.None);

const isExporting = ref(false);
const hasExportError = ref(false);

const availableSteps = ['export-1', 'export-2', 'create-export'];

const maxSteps = computed(() => {
  return availableSteps.length;
});

const currentNamedStep = computed(() => {
  return availableSteps[currentStep.value];
});

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');
  if (studyConfig.trackers.windowActivityTracker.enabled) {
    exportWindowActivitySelectedOption.value = DataExportType.All;
    mostRecentWindowActivities.value = await typedIpcRenderer.invoke(
      'getMostRecentWindowActivityDtos',
      5
    );
  }
  if (studyConfig.trackers.userInputTracker.enabled) {
    exportUserInputSelectedOption.value = DataExportType.All;
    mostRecentUserInputs.value = await typedIpcRenderer.invoke('getMostRecentUserInputDtos', 5);
  }
  isLoading.value = false;
});

async function handleWindowActivityExportConfigChanged(newSelectedOption: DataExportType) {
  if (mostRecentWindowActivities.value && newSelectedOption === DataExportType.Obfuscate) {
    mostRecentWindowActivitiesObfuscated.value = await typedIpcRenderer.invoke(
      'obfuscateWindowActivityDtosById',
      mostRecentWindowActivities.value.map((d) => d.id)
    );
    obfuscateWindowActivities.value = true;
  } else if (newSelectedOption === DataExportType.All) {
    obfuscateWindowActivities.value = false;
  }
  exportWindowActivitySelectedOption.value = newSelectedOption;
}

function handleUserInputExportConfigChanged(newSelectedOption: DataExportType) {
  exportUserInputSelectedOption.value = newSelectedOption;
}

async function handleNextStep() {
  if (currentStep.value === maxSteps.value - 1) {
    return;
  }
  transitionName.value = 'slide-left-right';
  currentStep.value++;
  if (currentNamedStep.value === 'create-export') {
    isExporting.value = true;
    try {
      await typedIpcRenderer.invoke(
        'startDataExport',
        exportWindowActivitySelectedOption.value,
        exportUserInputSelectedOption.value
      );
      hasExportError.value = false;
    } catch (e) {
      console.error(e);
      hasExportError.value = true;
    }
    isExporting.value = false;
  }
}

function handleBackStep() {
  if (currentStep.value === 0) {
    return;
  }
  transitionName.value = 'slide-right-left';
  currentStep.value--;
}

function openExportFolder(event: Event) {
  typedIpcRenderer.invoke('openExportFolder');
  event.preventDefault();
}
</script>

<template>
  <div class="h-screen p-5">
    <div
      v-if="!studyInfo || isExporting"
      class="flex h-full w-full items-center justify-center overflow-y-scroll"
    >
      <span class="loading loading-spinner loading-lg" />
    </div>
    <div v-else class="relative flex h-full flex-col justify-between text-neutral-400">
      <transition-group :name="transitionName">
        <div v-if="currentNamedStep === 'export-1'" key="0" class="flex w-full flex-col">
          <article class="prose prose-lg max-w-none">
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
            :data="
              obfuscateWindowActivities
                ? mostRecentWindowActivitiesObfuscated
                : mostRecentWindowActivities
            "
            :should-obfuscate="obfuscateWindowActivities"
            :default-value="exportWindowActivitySelectedOption"
            @change="handleWindowActivityExportConfigChanged"
          />
          <DataExportUserInputTracker
            v-if="studyConfig.trackers.windowActivityTracker.enabled"
            :study-info="studyInfo"
            :data="mostRecentUserInputs"
            :default-value="exportUserInputSelectedOption"
            @change="handleUserInputExportConfigChanged"
          />
        </div>
        <div v-if="currentNamedStep === 'create-export'" key="2" class="flex w-full flex-col">
          <article class="prose prose-lg max-w-none">
            <p>
              Thank you for reviewing and exporting your data for the study
              {{ studyConfig.name }}-study.
            </p>
            <p>
              A single password-protected and encrypted
              <b class="dark:text-white">file was created</b> based on your preferences on the
              previous page. To share this file with the researchers, please take the following
              steps:
            </p>
            <ol>
              <li>
                <a href="#" @click="openExportFolder">Click here</a> to open the folder containing
                your data-file (data-export.sqlite).
              </li>
              <li>
                <a :href="studyConfig.uploadUrl" target="_blank">Click here</a> to open the upload
                page.
              </li>
              <li>Upload the file named (data-export.sqlite) using the upload page.</li>
            </ol>
            <p>
              Please contact {{ studyConfig.contactName }} ({{ studyConfig.contactEmail }}) in case
              you have any questions. Thank you!
            </p>
            <p>
              If you want to review the complete data file before sharing it with the researchers,
              please refer to this guide. The password required for opening the exported file is:
              PASSWORD TODO.
            </p>
          </article>
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
