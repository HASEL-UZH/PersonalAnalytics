<script lang="ts" setup>
import typedIpcRenderer from '../utils/typedIpcRenderer';
import { onMounted, ref } from 'vue';
import { StudyInfoDto } from '../../shared/dto/StudyInfoDto';

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
    <article v-else class="prose-lg prose ml-6 mt-4 align-top">
      <h1 class="relative">
        <span class="primary-blue">{{ studyInfo.studyName }}</span>
        <span class="badge badge-neutral absolute top-0">v{{ studyInfo.appVersion }}</span>
      </h1>

      <h2 class="mt-0">Study Info</h2>
      <p>{{ studyInfo.shortDescription }}</p>
      <table class="table-auto">
        <tbody>
          <tr>
            <td>Your Subject Id:</td>
            <td>
              <span class="badge badge-neutral">{{ studyInfo.subjectId }}</span>
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

      <h2 class="mt-0">Monitoring Tool Info</h2>
      <p>TODO: Short summary of PersonalAnalytics</p>
      <table class="table-auto">
        <tbody>
          <tr>
            <td>Active Trackers:</td>
            <td>{{ studyInfo.currentlyActiveTrackers.join(', ') }}</td>
          </tr>
          <tr>
            <td>Contact:</td>
            <td>André Meyer (TBD), Sebastian Richner (TBD)</td>
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
              <a target="_blank">TODO: TBD</a>
            </td>
          </tr>
        </tbody>
      </table>
      <div class="float-end flex">
        <img src="../assets/logo_uzh.svg" class="mr-5 w-32 object-contain" />
        <img src="../assets/logo_hasel.svg" class="w-32 object-contain" />
      </div>
    </article>
  </div>
</template>
<style lang="less">
@import '../styles/index';
.primary-blue {
  color: @primary-color;
}
h1,
h2,
h3 {
  @apply !font-medium;
}
</style>
