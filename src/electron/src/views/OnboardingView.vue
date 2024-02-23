<script setup lang="ts">
import StepIndicator from '../components/StepIndicator.vue';
import { computed, onMounted, ref } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import typedIpcRenderer from '../utils/typedIpcRenderer';
import StudyInfo from '../components/StudyInfo.vue';

const currentStep = ref(0);
const currentPermissionImage = ref(0);
const transitionName = ref('slide-lef-right');
const isLoading = ref(false);

const availableSteps = ['welcome', 'data-collection'];
const maxSteps = computed(() => {
  return availableSteps.length;
});
const currentNamedStep = computed(() => {
  return availableSteps[currentStep.value];
});

const studyInfo = ref<StudyInfoDto>();
const permissionCheckInterval = ref<NodeJS.Timeout | null>();

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');
  setInterval(() => {
    switchPermissionImage();
  }, 3500);
});

function switchPermissionImage() {
  if (currentPermissionImage.value < 2) {
    currentPermissionImage.value++;
  } else {
    currentPermissionImage.value = 0;
  }
}

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
            <p class="mt-6">
              After closing this window, the
              <span class="font-bold text-slate-200">application may request access</span> to
              accessibility features and recording your screen.
            </p>
            <div class="relative flex h-52 items-center justify-center">
              <transition-group name="fade">
                <img
                  v-if="currentPermissionImage === 0"
                  src="../assets/onboarding/permissions1.png"
                  class="self-align-center absolute flex w-2/3"
                  alt="macOS Permissions"
                />
                <img
                  v-if="currentPermissionImage === 1"
                  src="../assets/onboarding/permissions2.png"
                  class="self-align-center absolute flex w-2/3"
                  alt="macOS Permissions"
                />
                <img
                  v-if="currentPermissionImage === 2"
                  src="../assets/onboarding/permissions3.png"
                  class="self-align-center absolute flex w-2/3"
                  alt="macOS Permissions"
                />
              </transition-group>
            </div>
            <p>
              Please open your System Settings (on macOS) and give PersonalAnalytics the required access.
              The application
              <span class="font-bold text-slate-200">may not work as intended</span> if you do not
              grant access.
            </p>
            <p class="mt-6">
              Please note: You might have to
              <span class="font-bold text-slate-200">manually restart</span> the application after
              granting access.
            </p>
            <div class="btn btn-primary mt-8" @click="startAllTrackers()">Grant Permission</div>
          </div>
          <div class="flex items-center justify-center"></div>
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
        <StepIndicator :current-step="currentStep" :total-steps="maxSteps" />
        <button
          class="btn btn-primary btn-md"
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
