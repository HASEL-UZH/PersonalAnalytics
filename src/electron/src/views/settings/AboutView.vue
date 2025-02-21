<script lang="ts" setup>
import typedIpcRenderer from '../../utils/typedIpcRenderer';
import { onMounted, ref } from 'vue';
import StudyInfoDto from '../../../shared/dto/StudyInfoDto';
import StudyInfo from '../../components/StudyInfo.vue';

const studyInfo = ref<StudyInfoDto>();

const openLogs = () => {
  typedIpcRenderer.invoke('openLogs');
};

const openCollectedData = () => {
  typedIpcRenderer.invoke('openCollectedData');
};

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');
});
</script>
<template>
  <div class="h-full overflow-y-scroll">
    <div v-if="!studyInfo" class="flex h-full w-full items-center justify-center">
      <span class="loading loading-spinner loading-lg" />
    </div>
    <div v-else class="mt-4">
      <article class="prose prose-lg mt-4">
        <h1 class="relative">
          <span class="primary-blue">{{ studyInfo.studyName }}</span>
          <span class="badge badge-neutral absolute top-0">v{{ studyInfo.appVersion }}</span>
        </h1>
      </article>

      <div class="z-10 mt-10 mb-10 flex items-center">
        <button class="btn btn-outline btn-sm mr-5" type="button" @click="openLogs">
          Open Logs
        </button>
        <button class="btn btn-outline btn-sm" type="button" @click="openCollectedData">
          Open Collected Data
        </button>
      </div>

      <StudyInfo :study-info="studyInfo" />

      <article class="prose prose-lg mt-4"> 
        <h2 class="mt-0">PersonalAnalytics Tool Info</h2>
        <p class="text-base">
          PersonalAnalytics is a software, developed by the Human Aspects of Software Engineering
          Lab of the University of Zurich to non-intrusively collect computer interaction data,
          store them locally to the user's machine, and allow users to voluntarily share a
          user-defined and potentially anonymized subset of the data with researchers for scientific
          purposes.
        </p>
        <table class="table-auto">
          <tbody>
            <tr>
              <td class="w-40">Active Trackers:</td>
              <td>{{ studyInfo.currentlyActiveTrackers.join(', ') }}</td>
            </tr>
            <tr>
              <td>Contact:</td>
              <td>Andre Meyer (ameyer@ifi.uzh.ch)</td>
            </tr>
            <tr>
              <td>Website:</td>
              <td>
                <a href="https://github.com/HASEL-UZH/PersonalAnalytics" target="_blank"
                  >https://github.com/HASEL-UZH/PersonalAnalytics</a
                >
              </td>
            </tr>
            <tr>
              <td>Privacy Policy:</td>
              <td>
                <a
                  href="https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev-am/documentation/PRIVACY.md"
                  target="_blank"
                  >https://github.com/HASEL-UZH/PersonalAnalytics/blob/dev-am/documentation/PRIVACY.md</a
                >
              </td>
            </tr>
          </tbody>
        </table>
        <p class="text-base">
          Various versions of PersonalAnalytics were thoroughly tested through more than a dozens field 
          studies involving hundreds of users. Even though the software runs reliably on most systems, we cannot exclude occasional 
          software issues. The use of this software is at the sole risk of the user. The creators of the software disclaim any 
          liability for damages or consequences, including but not limited to damages or losses arising from the use, 
          modification, or misuse of the software. The software is provided under an open-source license to researchers 
          and users <i>as-is</i> and can be inspected under the link provided above.
        </p>
        <div class="float-end flex pb-7">
          <a href="https://www.uzh.ch" target="_blank" class="mr-5">
            <img src="../../assets/logo_uzh.svg" class="m-0 w-44 object-contain" alt="UZH Logo" />
          </a>
          <a href="https://hasel.dev" target="_blank">
            <img src="../../assets/logo_hasel.svg" class="m-0 w-44 object-contain" alt="HASEL Logo" />
          </a>
        </div>
      </article>
    </div>
  </div>
</template>
<style lang="less">
@import '../../styles/index';
.primary-blue {
  color: @primary-color;
}
</style>
