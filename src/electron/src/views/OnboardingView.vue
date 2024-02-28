<script setup lang="ts">
import StepIndicator from '../components/StepIndicator.vue';
import { computed, onMounted, ref } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import typedIpcRenderer from '../utils/typedIpcRenderer';
import StudyInfo from '../components/StudyInfo.vue';
import { LocationQueryValue, useRoute } from 'vue-router';
import studyConfig from '../../shared/study.config';

const windowActivityTrackerEnabled = studyConfig.trackers.windowActivityTracker.enabled;
const requiresAccessibilityPermission =
  studyConfig.trackers.userInputTracker.enabled ||
  (windowActivityTrackerEnabled && studyConfig.trackers.windowActivityTracker.trackUrls);
const requiresScreenRecordingPermission =
  windowActivityTrackerEnabled && studyConfig.trackers.windowActivityTracker.trackWindowTitles;
const requiresAnyPermission = requiresAccessibilityPermission || requiresScreenRecordingPermission;

const currentStep = ref(0);
const transitionName = ref('slide-lef-right');
const isLoading = ref(false);

const studyInfo = ref<StudyInfoDto>();
const permissionCheckInterval = ref<NodeJS.Timeout | null>();
const hasAccessibilityPermission = ref(false);
const hasScreenRecordingPermission = ref(false);
const isAccessibilityPermissionLoading = ref(false);
const isScreenRecordingPermissionLoading = ref(false);

const route = useRoute();
const isMacOS: string | null | LocationQueryValue[] = route.query.isMacOS;
const goToStep: string | null | LocationQueryValue[] = route.query.goToStep;

const availableSteps = ['welcome'];

if (isMacOS === 'true' && requiresAnyPermission) {
  availableSteps.push('data-collection');
}

if (isMacOS === 'false') {
  availableSteps.push('study-trackers-started');
}

if (goToStep === 'study-trackers-started') {
  currentStep.value = availableSteps.indexOf('study-trackers-started');
}

const maxSteps = computed(() => {
  return availableSteps.length;
});

const currentNamedStep = computed(() => {
  return availableSteps[currentStep.value];
});

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');

  if (isMacOS && requiresAnyPermission) {
    permissionCheckInterval.value = setInterval(async () => {
      if (requiresAccessibilityPermission) {
        hasAccessibilityPermission.value = await triggerPermissionCheckAccessibility(false);
      }
      if (requiresScreenRecordingPermission) {
        hasScreenRecordingPermission.value = await triggerPermissionCheckScreenRecording();
      }
      if (isAccessibilityPermissionLoading.value && hasAccessibilityPermission.value) {
        isAccessibilityPermissionLoading.value = false;
      }
      if (isScreenRecordingPermissionLoading.value && hasScreenRecordingPermission.value) {
        isScreenRecordingPermissionLoading.value = false;
      }
    }, 1000);
  }
});

async function closeOnboardingWindow() {
  await typedIpcRenderer.invoke('closeOnboardingWindow');
}
async function handleNextStep() {
  if (currentStep.value === maxSteps.value - 1) {
    closeOnboardingWindow();
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

function requestAccessibilityPermission() {
  if (isAccessibilityPermissionLoading.value) {
    return;
  }
  isAccessibilityPermissionLoading.value = true;
  triggerPermissionCheckAccessibility(true);
}

async function requestScreenRecordingPermission(): Promise<void> {
  if (isScreenRecordingPermissionLoading.value) {
    return;
  }
  isScreenRecordingPermissionLoading.value = true;
  const hasPermission = await triggerPermissionCheckScreenRecording();
  if (!hasPermission) {
    startAllTrackers();
  }
}

function triggerPermissionCheckAccessibility(prompt: boolean): Promise<boolean> {
  return typedIpcRenderer.invoke('triggerPermissionCheckAccessibility', prompt);
}

function triggerPermissionCheckScreenRecording(): Promise<boolean> {
  return typedIpcRenderer.invoke('triggerPermissionCheckScreenRecording');
}

function startAllTrackers() {
  typedIpcRenderer.invoke('startAllTrackers');
}
</script>

<template>
  <div class="onboarding-view h-screen">
    <div v-if="!studyInfo" class="flex h-full w-full items-center justify-center">
      <span class="loading loading-spinner loading-lg" />
    </div>
    <div v-else class="relative flex h-full flex-col justify-between text-neutral-400">
      <transition-group :name="transitionName">
        <div v-if="currentNamedStep === 'welcome'" key="0" class="flex w-full flex-col">
          <div class="flex flex-row">
            <img
              class="self-center"
              src="../assets/logo.svg"
              alt="PersonalAnalytics Logo"
              width="80"
            />
            <h1 id="title" class="ml-5 self-center text-3xl font-normal text-neutral-300">
              Welcome to {{ studyInfo.studyName }}
            </h1>
          </div>
          <StudyInfo :study-info="studyInfo" />
        </div>
        <div v-else-if="currentNamedStep === 'data-collection'" key="1" class="absolute">
          <h1 class="mb-8 text-4xl font-medium text-neutral-300">Data Collection</h1>
          <div class="text-md">
            <p>
              While using PersonalAnalytics, we will store your application and website (title and
              url where available) usage. We only use this data to determine window titles and urls,
              your screen or audio will not be recorded. This
              <span class="font-bold text-slate-200">data is only stored locally</span> and does not
              leave your device.
            </p>
            <div class="flex flex-col">
              <div v-if="requiresAccessibilityPermission" class="my-5 flex flex-col">
                <div>
                  To correctly run PersonalAnalytics, we collect data on your window switches and
                  titles. To continue, click Open System Settings and enable Accessibility
                  permission for PersonalAnalytics.
                </div>
                <div class="flex items-center justify-center pt-8">
                  <button
                    v-if="!hasAccessibilityPermission"
                    class="btn btn-active w-64"
                    @click="requestAccessibilityPermission()"
                  >
                    <span v-if="isAccessibilityPermissionLoading">
                      <span class="loading loading-spinner loading-xs" />
                    </span>
                    <span v-else>Open Accessibility Settings</span>
                  </button>
                  <div v-else class="flex flex-row">
                    <svg
                      class="h-6 w-6 text-green-500"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M5 13l4 4L19 7"
                      />
                      <circle cx="12" cy="12" r="11" stroke="currentColor" stroke-width="2" />
                    </svg>

                    <span class="ml-2 text-green-500">Accessibility permission granted</span>
                  </div>
                </div>
              </div>
              <div v-if="requiresScreenRecordingPermission" class="my-5 flex flex-col">
                <div>
                  To correctly run PersonalAnalytics, we collect data on your window switches and
                  titles. To continue, click Open System Settings and enable Screen Recording
                  permission for PersonalAnalytics.
                </div>
                <div class="flex items-center justify-center pt-8">
                  <button
                    v-if="!hasScreenRecordingPermission"
                    class="btn btn-active w-64"
                    :disabled="requiresAccessibilityPermission && !hasAccessibilityPermission"
                    @click="requestScreenRecordingPermission()"
                  >
                    <span v-if="isScreenRecordingPermissionLoading">
                      <span class="loading loading-spinner loading-xs" />
                    </span>
                    <span v-else>Open Screen Settings</span>
                  </button>
                  <div v-else class="flex flex-row">
                    <svg
                      class="h-6 w-6 text-green-500"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M5 13l4 4L19 7"
                      />
                      <circle cx="12" cy="12" r="11" stroke="currentColor" stroke-width="2" />
                    </svg>

                    <span class="ml-2 text-green-500">Screen Recording permission granted</span>
                  </div>
                </div>
              </div>
            </div>
            <p v-if="requiresScreenRecordingPermission" class="mt-6">
              Please note: You will be asked to close and restart the application after granting
              access. If the application does not restart automatically, please do so manually.
            </p>
          </div>
        </div>
        <div v-else-if="currentNamedStep === 'study-trackers-started'" key="2" class="absolute">
          <h1 class="mb-8 text-4xl font-medium text-neutral-300">Data Collection</h1>
          <div class="text-md">
            <p v-if="requiresAnyPermission">
              PersonalAnalytics now has the necessary permissions to collect data and is collecting
              data in the background. You can manually open the application or view the collected
              data at any time by clicking the icon in the menu bar.
            </p>
            <p v-else>
              PersonalAnalytics is now collecting data. You can manually open the application or
              view the collected data at any time by right-clicking the icon in the menu bar.
            </p>
            <p>
              The following trackers are currently running:
              {{ studyInfo.currentlyActiveTrackers.join(', ') }}
            </p>
          </div>
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
<style lang="less" scoped>
.onboarding-view {
  padding: 25px;
}

.video-transition {
  transition: all 1s;
}

.video-transition.fading {
  opacity: 0;
}

.slide-left-right-enter-active,
.slide-right-left-enter-active,
.slide-left-right-leave-active,
.slide-right-left-leave-active {
  transition: all 0.25s ease-in-out;
}

.slide-left-right-leave-to {
  scale: 0.8;
  opacity: 0;
  transform: translateX(-100%);
}

.slide-left-right-enter-from {
  opacity: 0;
  scale: 0.8;
  transform: translateX(100%);
}

.slide-left-right-enter-to,
.slide-left-right-leave-from {
  opacity: 1;
  transform: translateX(0);
}

.slide-right-left-leave-to {
  scale: 0.8;
  opacity: 0;
  transform: translateX(100%);
}

.slide-right-left-enter-from {
  scale: 0.8;
  opacity: 0;
  transform: translateX(-100%);
}

.slide-right-left-enter-to,
.slide-right-left-leave-from {
  opacity: 1;
  transform: translateX(0);
}

.fade-enter-active,
.fade-leave-active {
  position: absolute;
  transition: all 1s ease-in-out;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
.fade-enter-to,
.fade-leave-from {
  opacity: 1;
}
</style>
