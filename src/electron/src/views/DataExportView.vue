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
import DataExportExperienceSamplingTracker from '../components/DataExportExperienceSamplingTracker.vue';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';
import getRendererLogger from '../utils/Logger';

const LOG = getRendererLogger('DataExportView');

const currentStep = ref(0);
const transitionName = ref('slide-lef-right');
const isLoading = ref(true);
const studyDescriptionExpanded = ref(false);

const studyInfo = ref<StudyInfoDto>();

const mostRecentExperienceSamples = ref<ExperienceSamplingDto[]>();
const mostRecentUserInputs = ref<UserInputDto[]>();
const mostRecentWindowActivities = ref<WindowActivityDto[]>();
const mostRecentWindowActivitiesObfuscated = ref<WindowActivityDto[]>();
const obfuscateWindowActivities = ref(false);

const exportExperienceSamplesSelectedOption = ref<DataExportType>(DataExportType.None);
const exportWindowActivitySelectedOption = ref<DataExportType>(DataExportType.None);
const exportUserInputSelectedOption = ref<DataExportType>(DataExportType.None);

const obfuscationTermsInput = ref<string[]>();

const isExporting = ref(false);
const hasExportError = ref(false);

const pathToExportedFile = ref('');
const fileName = ref('');

const availableSteps = ['export-1', 'export-2', 'create-export'];

const maxSteps = computed(() => {
  return availableSteps.length;
});

const currentNamedStep = computed(() => {
  return availableSteps[currentStep.value];
});

onMounted(async () => {
  studyInfo.value = (await typedIpcRenderer.invoke('getStudyInfo')) as StudyInfoDto;
  if (studyConfig.trackers.experienceSamplingTracker.enabled) {
    exportExperienceSamplesSelectedOption.value = DataExportType.All;
    mostRecentExperienceSamples.value = await typedIpcRenderer.invoke(
      'getMostRecentExperienceSamplingDtos',
      20
    );
  }
  if (studyConfig.trackers.windowActivityTracker.enabled) {
    exportWindowActivitySelectedOption.value = DataExportType.All;
    mostRecentWindowActivities.value = await typedIpcRenderer.invoke(
      'getMostRecentWindowActivityDtos',
      20
    );
  }
  if (studyConfig.trackers.userInputTracker.enabled) {
    exportUserInputSelectedOption.value = DataExportType.All;
    mostRecentUserInputs.value = await typedIpcRenderer.invoke('getMostRecentUserInputDtos', 20);
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
  } else if (
    newSelectedOption === DataExportType.All ||
    newSelectedOption === DataExportType.ObfuscateWithTerms
  ) {
    obfuscateWindowActivities.value = false;
    mostRecentWindowActivities.value = await typedIpcRenderer.invoke(
      'getMostRecentWindowActivityDtos',
      20
    );
  }
  exportWindowActivitySelectedOption.value = newSelectedOption;
}

async function handleObfuscationTermsChanged(newObfuscationTerms: string) {
  if (!newObfuscationTerms || newObfuscationTerms.trim().length === 0) {
    obfuscationTermsInput.value = [];
  } else {
    obfuscationTermsInput.value = newObfuscationTerms
      .split(',')
      .map((s: string) => s.trim())
      .filter((s: string) => s.length > 0);
  }
}

async function handleObfuscateSampleData() {
  if (
    obfuscationTermsInput.value &&
    obfuscationTermsInput?.value?.length > 0 &&
    mostRecentWindowActivities.value
  ) {
    mostRecentWindowActivities.value = mostRecentWindowActivities.value?.map((item) => {
      let windowTitle = item.windowTitle;
      let url = item.url;
      obfuscationTermsInput.value?.forEach((term) => {
        if (
          windowTitle?.toLowerCase().includes(term.toLowerCase()) ||
          url?.toLowerCase().includes(term.toLowerCase())
        ) {
          windowTitle = windowTitle ? '[anonymized]' : windowTitle;
          url = url ? '[anonymized]' : url;
        }
      });
      return { ...item, windowTitle, url };
    });
  } else {
    mostRecentWindowActivities.value = await typedIpcRenderer.invoke(
      'getMostRecentWindowActivityDtos',
      20
    );
  }
}

function handleExperienceSamplingConfigChanged(newSelectedOption: DataExportType) {
  exportExperienceSamplesSelectedOption.value = newSelectedOption;
}

function handleUserInputExportConfigChanged(newSelectedOption: DataExportType) {
  exportUserInputSelectedOption.value = newSelectedOption;
}

function closeDataExportWindow() {
  typedIpcRenderer.invoke('closeDataExportWindow');
}

async function handleNextStep() {
  if (currentStep.value === maxSteps.value - 1) {
    closeDataExportWindow();
    return;
  }

  transitionName.value = 'slide-left-right';
  currentStep.value++;
  if (currentNamedStep.value === 'create-export') {
    isExporting.value = true;
    try {
      let obfuscationTerms: string[] = [];
      if (
        exportWindowActivitySelectedOption.value === DataExportType.ObfuscateWithTerms &&
        obfuscationTermsInput.value &&
        obfuscationTermsInput.value.length > 0
      ) {
        obfuscationTerms = Array.from(obfuscationTermsInput.value);
      }
      pathToExportedFile.value = await typedIpcRenderer.invoke(
        'startDataExport',
        exportWindowActivitySelectedOption.value,
        exportUserInputSelectedOption.value,
        obfuscationTerms,
        studyConfig.dataExportEncrypted
      );
      hasExportError.value = false;
      const now = new Date();
      const nowStr = now.toISOString().replace(/:/g, '-').replace('T', '_').slice(0, 16);
      // Also update the DataExportService if you change the file name here
      fileName.value = `PA_${studyInfo.value?.subjectId}_${nowStr}.sqlite`;
    } catch (e) {
      LOG.error(e);
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

function openUploadUrl(event: Event) {
  typedIpcRenderer.invoke('openUploadUrl');
  event.preventDefault();
}

function revealItemInFolder(event: Event) {
  typedIpcRenderer.invoke('revealItemInFolder', pathToExportedFile.value);
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
    <div v-else class="relative flex h-full flex-col justify-between dark:text-neutral-400">
      <div class="mb-5 flex-grow overflow-y-auto">
        <transition-group :name="transitionName">
          <div v-if="currentNamedStep === 'export-1'" key="0" class="flex w-full flex-col">
            <h1 class="mb-8 text-4xl font-medium text-neutral-800 dark:text-neutral-300">
              Data Export
            </h1>
            <article class="prose prose-lg max-w-none">
              <p>
                Thank you for participating in the {{ studyConfig.name }}-study! So far, all data
                that has been collected and stored
                <b class="dark:text-white">only locally on your machine</b>. In this step, the
                researchers would like to ask you to share this data for analysis and publication in
                scientific journals.
              </p>
              <p>
                Please click <b class="dark:text-white">"Next"</b> once you are ready to
                <b class="dark:text-white">first review and later share your data</b>.
                <span v-if="studyConfig.dataExportEncrypted">
                  The export that will be created with your permission in the next step will be
                  encrypted and password-protected. </span
                >
              </p>
              <p class="mb-4">
                Below, you find additional information on the study and how the researchers ensure
                your data privacy and security.
              </p>
              <table class="table-auto text-sm">
                <tbody>
                  <tr>
                    <td>Contact:</td>
                    <td>{{ studyInfo.contactName }} (<a :href="'mailto:' + studyInfo.contactEmail" target="_blank">{{ studyInfo.contactEmail }}</a>)</td>
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
                  <tr>
                    <td class="w-40 align-top">Study Description:</td>
                    <td>
                      <!-- <div class="collapse bg-base-200">
                        <input v-model="studyDescriptionExpanded" type="checkbox" />
                        <div class="collapse-title text-sm">
                          Click to {{ studyDescriptionExpanded ? 'collapse' : 'expand' }} Study
                          Description
                        </div>
                        <div class="collapse-content" v-html="studyInfo.shortDescription" />
                      </div> -->
                      <div v-html="studyInfo.shortDescription"></div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </article>
          </div>
          <div v-if="currentNamedStep === 'create-export'" key="2" class="flex w-full flex-col">
            <h1 class="mb-8 text-4xl font-medium text-neutral-800 dark:text-neutral-300">
              Your Export is Ready
            </h1>
            <article class="prose prose-lg max-w-none">
              <p>
                Thank you for reviewing and exporting your data for the study
                {{ studyConfig.name }}.
              </p>
              <p>
                Your data was exported and we created a
                <span v-if="studyConfig.dataExportEncrypted"
                  >password-protected and encrypted
                </span>
                file based on your preferences on the previous page. To share this file with the
                researchers, please take the following steps:
              </p>
              <ol>
                <li>
                  <a href="#" @click="revealItemInFolder">Click here</a> to open the folder
                  containing your data-file (<span
                    class="badge badge-neutral font-bold text-white"
                    >{{ fileName }}</span
                  >).
                </li>
                <li>
                  <a href="#" @click="openUploadUrl">Click here</a> to open the upload
                  page.
                </li>
                <li>
                  Upload the file named
                  <span class="badge badge-neutral font-bold text-white">{{ fileName }}</span> using
                  the upload page.
                </li>
              </ol>
              <p>
                Please contact {{ studyConfig.contactName }} ({{ studyConfig.contactEmail }}) in
                case you have any questions. Thank you!
              </p>
              <p v-if="studyConfig.dataExportEncrypted">
                If you want to review the complete data file before sharing it with the researchers,
                please refer to this guide. The <b class="dark:text-white">password</b> required for
                opening the exported file is:
                <span class="password-badge">PersonalAnalytics_{{ studyInfo.subjectId }}</span
                >.
              </p>
            </article>
          </div>
          <div v-if="currentNamedStep === 'export-2'" key="1" class="-mt-5">
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
              @option-changed="handleWindowActivityExportConfigChanged"
              @obfuscation-terms-changed="handleObfuscationTermsChanged"
              @obfuscate-sample-data="handleObfuscateSampleData"
            />
            <DataExportUserInputTracker
              v-if="studyConfig.trackers.windowActivityTracker.enabled"
              :study-info="studyInfo"
              :data="mostRecentUserInputs"
              :default-value="exportUserInputSelectedOption"
              @change="handleUserInputExportConfigChanged"
            />
            <DataExportExperienceSamplingTracker
              v-if="studyConfig.trackers.windowActivityTracker.enabled"
              :study-info="studyInfo"
              :data="mostRecentExperienceSamples"
              :default-value="exportExperienceSamplesSelectedOption"
              @change="handleExperienceSamplingConfigChanged"
            />
          </div>
        </transition-group>
      </div>
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
<style lang="less" scoped>
@import '../styles/variables.less';
.password-badge {
  @apply badge badge-neutral font-bold text-white;
  background-color: @primary-color;
}
</style>
