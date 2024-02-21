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
  <div class="h-full p-4">
    <!-- center spinner across the screen -->
    <div v-if="!studyInfo" class="flex h-full items-center justify-center">
      <span class="loading loading-spinner loading-lg"></span>
    </div>
    <article v-else class="lg:prose-lg prose mx-auto mt-8 align-top">
      <h1>
        <span class="primary-blue">{{ studyInfo.studyName }}</span>
      </h1>
      <div class="badge badge-neutral fixed right-0 top-0 mr-4 mt-4">
        Version {{ studyInfo.appVersion }}
      </div>
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
            <td>{{ studyInfo.infoUrl }}</td>
          </tr>
          <tr>
            <td>Privacy Policy:</td>
            <td>{{ studyInfo.privacyPolicyUrl }}</td>
          </tr>
          <tr>
            <td>Active Trackers:</td>
            <td>{{ studyInfo.currentlyActiveTrackers.join(', ') }}</td>
          </tr>
        </tbody>
      </table>
    </article>
  </div>
</template>
<style lang="less">
@import '../styles/index';
.primary-blue {
  color: @primary-color;
}
</style>
