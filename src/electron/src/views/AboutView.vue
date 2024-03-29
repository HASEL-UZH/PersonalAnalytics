<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import { onMounted, ref } from 'vue';
import StudyInfoDto from '../../shared/dto/StudyInfoDto';
import StudyInfo from '../components/StudyInfo.vue';

const studyInfo = ref<StudyInfoDto>();

onMounted(async () => {
  studyInfo.value = await typedIpcRenderer.invoke('getStudyInfo');
});
</script>
<template>
  <div class="h-full overflow-y-scroll">
    <div v-if="!studyInfo" class="flex h-full w-full items-center justify-center">
      <span class="loading loading-spinner loading-lg" />
    </div>
    <div v-else class="ml-6 mt-4">
      <article class="prose prose-lg mt-4">
        <h1 class="relative">
          <span class="primary-blue">{{ studyInfo.studyName }}</span>
          <span class="badge badge-neutral absolute top-0">v{{ studyInfo.appVersion }}</span>
        </h1>
      </article>

      <StudyInfo :study-info="studyInfo" />

      <article class="prose prose-lg mt-4">
        <h2 class="mt-0">Monitoring Tool Info</h2>
        <p>
          PersonalAnalytics is a software, developed by the Human Aspects of Software Engineering
          Lab of the University of Zurich to non-intrusively collect computer interaction data,
          store them locally o the user's machine, and allow users to voluntarily share a
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
        <div class="float-end flex pb-7">
          <a href="https://www.uzh.ch" target="_blank" class="mr-5">
            <img src="../assets/logo_uzh.svg" class="m-0 w-32 object-contain" alt="UZH Logo" />
          </a>
          <a href="https://hasel.dev" target="_blank">
            <img src="../assets/logo_hasel.svg" class="m-0 w-32 object-contain" alt="HASEL Logo" />
          </a>
        </div>
      </article>
    </div>
  </div>
</template>
<style lang="less">
@import '../styles/index';
.primary-blue {
  color: @primary-color;
}
</style>
